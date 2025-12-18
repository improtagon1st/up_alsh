using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using up_melnilov_522.Helpers;

namespace up_melnilov_522.Windows
{
    public partial class OrderEditWindow : Window
    {
        private readonly int? _orderId;

        public OrderEditWindow(int? orderId)
        {
            InitializeComponent();

            _orderId = orderId;

            CbStatus.ItemsSource = new[]
            {
                "Новый",
                "В сборке",
                "Доставлен",
                "Выдан",
                "Отменен"
            };
            CbStatus.SelectedIndex = 0;

            if (_orderId == null)
            {
                Title = "Добавить заказ";
                LblArticle.Visibility = Visibility.Collapsed;
                TbArticle.Visibility = Visibility.Collapsed;

                DpOrderDate.SelectedDate = DateTime.Today;
                DpDeliveryDate.SelectedDate = DateTime.Today;
            }
            else
            {
                Title = "Редактировать заказ";
                LoadOrder();
            }
        }

        private void LoadOrder()
        {
            try
            {
                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    // PickupAddress читаем через SQL, чтобы не трогать EDMX
                    string sql =
                        "SELECT o.OrderID, o.OrderStatus, o.OrderDate, o.DeliveryDate, o.PickupAddress " +
                        "FROM Orders o WHERE o.OrderID = @p0";

                    var row = db.Database.SqlQuery<OrderRow>(sql, _orderId.Value).FirstOrDefault();
                    if (row == null)
                    {
                        UiHelper.ShowError("Заказ не найден. Возможно, он был удалён.", "Ошибка");
                        DialogResult = false;
                        Close();
                        return;
                    }

                    TbArticle.Text = row.OrderID.ToString();

                    if (!string.IsNullOrWhiteSpace(row.OrderStatus))
                        CbStatus.SelectedItem = row.OrderStatus;

                    TbPickupAddress.Text = row.PickupAddress ?? "";

                    DpOrderDate.SelectedDate = row.OrderDate ?? DateTime.Today;
                    DpDeliveryDate.SelectedDate = row.DeliveryDate ?? DateTime.Today;
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось загрузить заказ.", ex));
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Role != "Администратор")
            {
                UiHelper.ShowWarning("Добавление/редактирование заказов доступно только администратору.", "Запрещено");
                return;
            }

            try
            {
                string status = (CbStatus.SelectedItem as string) ?? "Новый";
                string address = TbPickupAddress.Text.Trim();

                if (string.IsNullOrWhiteSpace(address))
                {
                    UiHelper.ShowWarning("Введите адрес пункта выдачи.", "Ошибка ввода");
                    return;
                }

                DateTime? orderDate = DpOrderDate.SelectedDate;
                DateTime? deliveryDate = DpDeliveryDate.SelectedDate;

                if (orderDate == null || deliveryDate == null)
                {
                    UiHelper.ShowWarning("Выберите даты заказа и выдачи.", "Ошибка ввода");
                    return;
                }

                if (deliveryDate.Value.Date < orderDate.Value.Date)
                {
                    UiHelper.ShowWarning("Дата выдачи не может быть раньше даты заказа.", "Ошибка ввода");
                    return;
                }

                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    // Комментарий: в таблице Orders есть обязательный UserID, но в ТЗ поле не указано.
                    // Поэтому ставим текущего пользователя (кто вошёл).
                    int userId = UserSession.CurrentUser != null ? UserSession.CurrentUser.UserID : 1;

                    if (_orderId == null)
                    {
                        // INSERT через SQL, чтобы писать PickupAddress без EDMX
                        string ins =
                            "INSERT INTO Orders (OrderDate, DeliveryDate, UserID, OrderStatus, PickupAddress) " +
                            "VALUES (@p0, @p1, @p2, @p3, @p4)";

                        db.Database.ExecuteSqlCommand(ins, orderDate, deliveryDate, userId, status, address);
                    }
                    else
                    {
                        string upd =
                            "UPDATE Orders SET OrderDate=@p0, DeliveryDate=@p1, OrderStatus=@p2, PickupAddress=@p3 " +
                            "WHERE OrderID=@p4";

                        db.Database.ExecuteSqlCommand(upd, orderDate, deliveryDate, status, address, _orderId.Value);
                    }
                }

                UiHelper.ShowInfo("Заказ сохранён.", "Готово");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось сохранить заказ.", ex));
            }
        }

        private class OrderRow
        {
            public int OrderID { get; set; }
            public DateTime? OrderDate { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public string OrderStatus { get; set; }
            public string PickupAddress { get; set; }
        }
    }
}
