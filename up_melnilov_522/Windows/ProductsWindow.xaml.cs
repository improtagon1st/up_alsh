using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using up_melnilov_522.Helpers;
using up_melnilov_522.Models;

namespace up_melnilov_522.Windows
{
    public partial class ProductsWindow : Window
    {
        private ProductCardForList[] _allProducts;
        private bool _isEditWindowOpen;

        public ProductsWindow()
        {
            InitializeComponent();

            TbUser.Text = UserSession.FullName;

            CbStockSort.ItemsSource = new[]
            {
                "Без сортировки",
                "Остаток (возр.)",
                "Остаток (убыв.)"
            };
            CbStockSort.SelectedIndex = 0;

            ApplyRoleUi();
            LoadSuppliers();
            LoadProducts();

            LvProducts.Loaded += LvProducts_Loaded;
        }

        private void ApplyRoleUi()
        {
            string role = UserSession.Role;

            bool canSearch = role == "Менеджер" || role == "Администратор";
            PanelControls.Visibility = canSearch ? Visibility.Visible : Visibility.Collapsed;

            BtnOrders.Visibility = canSearch ? Visibility.Visible : Visibility.Collapsed;

            bool isAdmin = role == "Администратор";
            BtnAdd.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnDelete.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadSuppliers()
        {
            try
            {
                CbSupplier.Items.Clear();
                CbSupplier.Items.Add("Все поставщики");

                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    var suppliers = db.Suppliers
                        .Select(s => s.SupplierName)
                        .OrderBy(s => s)
                        .ToList();

                    foreach (var s in suppliers)
                        CbSupplier.Items.Add(s);
                }

                CbSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось загрузить список поставщиков.", ex));
            }
        }

        private void LoadProducts()
        {
            try
            {
                List<ProductCardForList> list = new List<ProductCardForList>();

                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    var query =
                        from p in db.Products
                        join c in db.Categories on p.CategoryID equals c.CategoryID
                        join m in db.Manufacturers on p.ManufacturerID equals m.ManufacturerID
                        join s in db.Suppliers on p.SupplierID equals s.SupplierID
                        select new
                        {
                            p.ProductID,
                            p.ProductName,
                            CategoryName = c.CategoryName,
                            p.Description,
                            ManufacturerName = m.ManufacturerName,
                            SupplierName = s.SupplierName,
                            p.UnitPrice,
                            p.Discount,
                            p.UnitOfMeasure,
                            p.StockQuantity,
                            p.Photo
                        };

                    var data = query.ToList();

                    foreach (var x in data)
                    {
                        int discount = x.Discount ?? 0;
                        int stock = x.StockQuantity ?? 0;

                        decimal price = x.UnitPrice ?? 0m;
                        decimal finalPrice = price * (100 - discount) / 100m;

                        var card = new ProductCardForList();
                        card.ProductID = x.ProductID;

                        card.ProductName = x.ProductName ?? "";
                        card.CategoryName = x.CategoryName ?? "";
                        card.Description = x.Description ?? "";

                        card.ManufacturerNameRaw = x.ManufacturerName ?? "";
                        card.SupplierNameRaw = x.SupplierName ?? "";
                        card.UnitOfMeasureRaw = x.UnitOfMeasure ?? "";
                        card.PhotoPathRaw = x.Photo ?? "";

                        card.ManufacturerLine = "Производитель: " + card.ManufacturerNameRaw;
                        card.SupplierLine = "Поставщик: " + card.SupplierNameRaw;

                        card.UnitLine = "Ед. изм.: " + card.UnitOfMeasureRaw;
                        card.StockLine = "Количество на складе: " + stock;

                        card.DiscountLine = "Действующая скидка: " + discount + "%";
                        card.Discount = discount;
                        card.StockQuantity = stock;

                        card.HasDiscount = discount > 0;
                        card.OldPriceText = card.HasDiscount ? price.ToString("F2") : "";
                        card.PriceText = (card.HasDiscount ? finalPrice : price).ToString("F2", CultureInfo.InvariantCulture);

                        card.PhotoImage = ImageHelper.GetImageOrStub(x.Photo);

                        list.Add(card);
                    }
                }

                _allProducts = list.ToArray();
                ApplySearchFilterSort();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось загрузить товары.", ex));
            }
        }

        private void LvProducts_Loaded(object sender, RoutedEventArgs e)
        {
            PaintRows();
        }

        private void PaintRows()
        {
            for (int i = 0; i < LvProducts.Items.Count; i++)
            {
                var item = LvProducts.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                if (item == null) continue;

                var p = LvProducts.Items[i] as ProductCardForList;
                if (p == null) continue;

                if (p.StockQuantity <= 0)
                {
                    item.Background = new SolidColorBrush(Colors.LightSkyBlue);
                }
                else if (p.Discount > 15)
                {
                    item.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#2E8B57"));
                }
                else
                {
                    item.Background = Brushes.White;
                }
            }
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilterSort();
        }

        private void CbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySearchFilterSort();
        }

        private void CbStockSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplySearchFilterSort();
        }

        private void ApplySearchFilterSort()
        {
            if (_allProducts == null)
            {
                LvProducts.ItemsSource = null;
                return;
            }

            var items = _allProducts;

            // Фильтр по поставщику
            string supplier = CbSupplier.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(supplier) && supplier != "Все поставщики")
            {
                items = items.Where(p => (p.SupplierNameRaw ?? "") == supplier).ToArray();
            }

            // Поиск по всем текстовым полям + по нескольким словам
            string q = TbSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(q))
            {
                string[] tokens = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                items = items.Where(p =>
                {
                    string allText =
                        (p.ProductName ?? "") + " " +
                        (p.CategoryName ?? "") + " " +
                        (p.Description ?? "") + " " +
                        (p.ManufacturerNameRaw ?? "") + " " +
                        (p.SupplierNameRaw ?? "") + " " +
                        (p.UnitOfMeasureRaw ?? "") + " " +
                        (p.PhotoPathRaw ?? "");

                    allText = allText.ToLower();
                    return tokens.All(t => allText.Contains(t));
                }).ToArray();
            }

            // Сортировка по остатку (возр/убыв)
            if (CbStockSort.SelectedIndex == 1)
                items = items.OrderBy(p => p.StockQuantity).ToArray();
            else if (CbStockSort.SelectedIndex == 2)
                items = items.OrderByDescending(p => p.StockQuantity).ToArray();

            LvProducts.ItemsSource = items;
            PaintRows();
        }

        private void LvProducts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UserSession.Role != "Администратор")
                return;

            var selected = LvProducts.SelectedItem as ProductCardForList;
            if (selected == null) return;

            OpenEditWindow(selected.ProductID);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenEditWindow(null);
        }

        private void OpenEditWindow(int? productId)
        {
            if (_isEditWindowOpen)
            {
                UiHelper.ShowWarning("Окно редактирования уже открыто. Закройте его, чтобы продолжить.", "Запрещено");
                return;
            }

            try
            {
                _isEditWindowOpen = true;

                var win = new ProductEditWindow(productId);
                win.Owner = this;

                bool? result = win.ShowDialog();
                if (result == true)
                {
                    LoadSuppliers();
                    LoadProducts();
                }
            }
            finally
            {
                _isEditWindowOpen = false;
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Role != "Администратор")
            {
                UiHelper.ShowWarning("Удаление доступно только администратору.", "Запрещено");
                return;
            }

            var selected = LvProducts.SelectedItem as ProductCardForList;
            if (selected == null)
            {
                UiHelper.ShowWarning("Выберите товар в списке.", "Нет выбора");
                return;
            }

            bool ok = UiHelper.AskYesNo("Удалить выбранный товар?\nОперация необратима.", "Удаление товара");
            if (!ok) return;

            try
            {
                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    bool inOrder = db.OrderDetails.Any(d => d.ProductID == selected.ProductID);
                    if (inOrder)
                    {
                        UiHelper.ShowWarning(
                            "Нельзя удалить товар, который присутствует в заказе.\nСначала удалите его из заказов.",
                            "Запрещено");
                        return;
                    }

                    var p = db.Products.FirstOrDefault(x => x.ProductID == selected.ProductID);
                    if (p == null)
                    {
                        UiHelper.ShowWarning("Товар не найден. Обновите список.", "Ошибка данных");
                        return;
                    }

                    string photo = p.Photo;
                    if (!string.IsNullOrWhiteSpace(photo))
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photo.Replace("/", "\\"));
                        if (File.Exists(fullPath))
                        {
                            try { File.Delete(fullPath); } catch { }
                        }
                    }

                    db.Products.Remove(p);
                    db.SaveChanges();
                }

                UiHelper.ShowInfo("Товар успешно удалён.", "Готово");
                LoadProducts();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось удалить товар.", ex));
            }
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            // Окно заказов доступно менеджеру и администратору
            if (UserSession.Role != "Менеджер" && UserSession.Role != "Администратор")
            {
                UiHelper.ShowWarning("Просмотр заказов доступен только менеджеру и администратору.", "Запрещено");
                return;
            }

            try
            {
                var win = new OrdersWindow();
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось открыть окно заказов.", ex));
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            var w = new LoginWindow();
            w.Show();
            Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;
            var w = new LoginWindow();
            w.Show();
            Close();
        }
    }
}
