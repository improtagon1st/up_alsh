using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using up_melnilov_522.Helpers;

namespace up_melnilov_522.Windows
{
    public partial class ProductEditWindow : Window
    {
        private readonly int? _productId;

        private string _oldPhotoFromDb;
        private string _selectedPhotoFile;
        private bool _photoChanged;

        public ProductEditWindow(int? productId)
        {
            InitializeComponent();

            _productId = productId;

            if (_productId == null)
            {
                Title = "Добавление товара";

                // ID при добавлении не показываем
                LblId.Visibility = Visibility.Collapsed;
                TbId.Visibility = Visibility.Collapsed;
            }
            else
            {
                Title = "Редактирование товара";

                // ID при редактировании только чтение (в XAML уже IsReadOnly=True)
                LblId.Visibility = Visibility.Visible;
                TbId.Visibility = Visibility.Visible;
            }

            LoadCombos();
            LoadData();
        }

        private void LoadCombos()
        {
            try
            {
                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    var categories = db.Categories
                        .Select(c => new { c.CategoryID, c.CategoryName })
                        .OrderBy(c => c.CategoryName)
                        .ToList();

                    CbCategory.ItemsSource = categories;
                    CbCategory.DisplayMemberPath = "CategoryName";
                    CbCategory.SelectedValuePath = "CategoryID";
                    if (categories.Count > 0) CbCategory.SelectedIndex = 0;

                    var manufacturers = db.Manufacturers
                        .Select(m => new { m.ManufacturerID, m.ManufacturerName })
                        .OrderBy(m => m.ManufacturerName)
                        .ToList();

                    CbManufacturer.ItemsSource = manufacturers;
                    CbManufacturer.DisplayMemberPath = "ManufacturerName";
                    CbManufacturer.SelectedValuePath = "ManufacturerID";
                    if (manufacturers.Count > 0) CbManufacturer.SelectedIndex = 0;

                    var suppliers = db.Suppliers
                        .Select(s => new { s.SupplierID, s.SupplierName })
                        .OrderBy(s => s.SupplierName)
                        .ToList();

                    CbSupplier.ItemsSource = suppliers;
                    CbSupplier.DisplayMemberPath = "SupplierName";
                    CbSupplier.SelectedValuePath = "SupplierID";
                    if (suppliers.Count > 0) CbSupplier.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText(
                    "Не удалось загрузить справочники (категории/производители/поставщики).", ex));
            }
        }

        private void LoadData()
        {
            try
            {
                ImgPhoto.Source = ImageHelper.GetImageOrStub(null);
                TbPhotoInfo.Text = "";
                TbPhotoPath.Text = "";

                if (_productId == null)
                {
                    TbDiscount.Text = "0";
                    TbStock.Text = "0";
                    TbPrice.Text = "0";
                    return;
                }

                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    var p = db.Products.FirstOrDefault(x => x.ProductID == _productId.Value);
                    if (p == null)
                    {
                        UiHelper.ShowError("Товар не найден. Возможно, он был удалён.", "Ошибка");
                        DialogResult = false;
                        Close();
                        return;
                    }

                    TbId.Text = p.ProductID.ToString();

                    TbName.Text = p.ProductName ?? "";
                    TbDescription.Text = p.Description ?? "";
                    TbUnit.Text = p.UnitOfMeasure ?? "";

                    TbPrice.Text = (p.UnitPrice ?? 0m).ToString("F2", CultureInfo.InvariantCulture);
                    TbDiscount.Text = (p.Discount ?? 0).ToString();
                    TbStock.Text = (p.StockQuantity ?? 0).ToString();

                    CbCategory.SelectedValue = p.CategoryID;
                    CbManufacturer.SelectedValue = p.ManufacturerID;
                    CbSupplier.SelectedValue = p.SupplierID;

                    _oldPhotoFromDb = p.Photo;
                    TbPhotoPath.Text = _oldPhotoFromDb ?? "";

                    ImgPhoto.Source = ImageHelper.GetImageOrStub(_oldPhotoFromDb);
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось загрузить данные товара.", ex));
            }
        }

        private void BtnChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp";
                dlg.Title = "Выбор изображения";

                bool? result = dlg.ShowDialog();
                if (result != true) return;

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(dlg.FileName, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                if (bmp.PixelWidth > 300 || bmp.PixelHeight > 200)
                {
                    UiHelper.ShowWarning(
                        "Изображение слишком большое.\n\n" +
                        "Требование: не больше 300×200 пикселей.\n" +
                        $"Ваше: {bmp.PixelWidth}×{bmp.PixelHeight}.\n\n" +
                        "Выберите другое изображение или уменьшите его в редакторе.",
                        "Ограничение изображения");
                    return;
                }

                _selectedPhotoFile = dlg.FileName;
                _photoChanged = true;

                ImgPhoto.Source = bmp;
                TbPhotoInfo.Text = "Выбрано: " + Path.GetFileName(_selectedPhotoFile);
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось загрузить изображение.", ex));
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UserSession.Role != "Администратор")
                {
                    UiHelper.ShowWarning("Добавление/редактирование доступно только администратору.", "Запрещено");
                    return;
                }

                string name = TbName.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    UiHelper.ShowWarning("Введите наименование товара.", "Ошибка ввода");
                    return;
                }

                if (CbCategory.SelectedValue == null || CbManufacturer.SelectedValue == null || CbSupplier.SelectedValue == null)
                {
                    UiHelper.ShowWarning("Выберите категорию, производителя и поставщика.", "Ошибка ввода");
                    return;
                }

                if (!decimal.TryParse(TbPrice.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                {
                    UiHelper.ShowWarning("Цена введена неверно. Пример: 1999.50", "Ошибка ввода");
                    return;
                }
                if (price < 0)
                {
                    UiHelper.ShowWarning("Цена не может быть отрицательной.", "Ошибка ввода");
                    return;
                }

                if (!int.TryParse(TbStock.Text.Trim(), out int stock))
                {
                    UiHelper.ShowWarning("Количество на складе введено неверно.", "Ошибка ввода");
                    return;
                }
                if (stock < 0)
                {
                    UiHelper.ShowWarning("Количество на складе не может быть отрицательным.", "Ошибка ввода");
                    return;
                }

                if (!int.TryParse(TbDiscount.Text.Trim(), out int discount))
                {
                    UiHelper.ShowWarning("Скидка введена неверно.", "Ошибка ввода");
                    return;
                }
                if (discount < 0)
                {
                    UiHelper.ShowWarning("Скидка не может быть отрицательной.", "Ошибка ввода");
                    return;
                }

                string desc = TbDescription.Text.Trim();
                string unit = TbUnit.Text.Trim();

                int categoryId = (int)CbCategory.SelectedValue;
                int manufacturerId = (int)CbManufacturer.SelectedValue;
                int supplierId = (int)CbSupplier.SelectedValue;

                string newPhotoDbPath = _oldPhotoFromDb;

                if (_photoChanged && !string.IsNullOrWhiteSpace(_selectedPhotoFile))
                {
                    string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    Directory.CreateDirectory(imagesDir);

                    string ext = Path.GetExtension(_selectedPhotoFile);
                    if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

                    string newFileName = Guid.NewGuid().ToString("N") + ext;
                    string destFullPath = Path.Combine(imagesDir, newFileName);

                    File.Copy(_selectedPhotoFile, destFullPath, true);

                    newPhotoDbPath = "Images/" + newFileName;

                    if (!string.IsNullOrWhiteSpace(_oldPhotoFromDb))
                    {
                        string oldFull = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _oldPhotoFromDb.Replace("/", "\\"));
                        if (File.Exists(oldFull))
                        {
                            try { File.Delete(oldFull); } catch { }
                        }
                    }
                }

                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    Products p;

                    if (_productId == null)
                    {
                        p = new Products();
                        db.Products.Add(p);
                    }
                    else
                    {
                        p = db.Products.FirstOrDefault(x => x.ProductID == _productId.Value);
                        if (p == null)
                        {
                            UiHelper.ShowError("Товар не найден. Возможно, он был удалён.", "Ошибка");
                            return;
                        }
                    }

                    p.ProductName = name;
                    p.Description = desc;
                    p.UnitOfMeasure = unit;

                    p.UnitPrice = price;
                    p.Discount = discount;
                    p.StockQuantity = stock;

                    p.CategoryID = categoryId;
                    p.ManufacturerID = manufacturerId;
                    p.SupplierID = supplierId;

                    p.Photo = newPhotoDbPath;

                    db.SaveChanges();
                }

                UiHelper.ShowInfo("Данные товара сохранены.", "Готово");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось сохранить товар.", ex));
            }
        }
    }
}
