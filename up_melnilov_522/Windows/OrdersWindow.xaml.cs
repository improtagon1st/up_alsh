using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using up_melnilov_522.Helpers;
using up_melnilov_522.Models;

namespace up_melnilov_522.Windows
{
    public partial class OrdersWindow : Window
    {
        private OrderCardForList[] _allOrders;

        public OrdersWindow()
        {
            InitializeComponent();

            TbUser.Text = UserSession.FullName;

            ApplyRoleUi();

            try
            {
                EnsurePickupAddressColumn();
                LoadOrders();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось открыть список заказов.", ex));
            }
        }

        private void ApplyRoleUi()
        {
            bool isAdmin = UserSession.Role == "Администратор";

            BtnAddOrder.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnDeleteOrder.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EnsurePickupAddressColumn()
        {
            // Комментарий: в исходной БД нет поля "адрес пункта выдачи". Добавляем колонку, если её нет.
            using (var db = new Shoes_Melnikovv_522Entitiess())
            {
                string sql =
                    "IF COL_LENGTH('Orders', 'PickupAddress') IS NULL " +
                    "BEGIN ALTER TABLE Orders ADD PickupAddress NVARCHAR(200) NULL END";

                db.Database.ExecuteSqlCommand(sql);
            }
        }

        private void LoadOrders()
        {
            List<OrderCardForList> list = new List<OrderCardForList>();

            using (var db = new Shoes_Melnikovv_522Entitiess())
            {
                // Берём через SQL, чтобы читать PickupAddress без обновления EDMX
                string sql =
                    "SELECT o.OrderID, o.OrderStatus, o.OrderDate, o.DeliveryDate, o.PickupAddress " +
                    "FROM Orders o ORDER BY o.OrderID DESC";

                var rows = db.Database.SqlQuery<OrderRow>(sql).ToList();

                foreach (var r in rows)
                {
                    var card = new OrderCardForList();
                    card.OrderID = r.OrderID;

                    card.OrderStatus = r.OrderStatus ?? "";
                    card.PickupAddress = r.PickupAddress ?? "";

                    card.OrderDate = r.OrderDate;
                    card.DeliveryDate = r.DeliveryDate;

                    card.ArticleLine = "Артикул заказа: " + r.OrderID;
                    card.StatusLine = "Статус заказа: " + (string.IsNullOrWhiteSpace(r.OrderStatus) ? "-" : r.OrderStatus);
                    card.PickupAddressLine = "Адрес пункта выдачи: " + (string.IsNullOrWhiteSpace(r.PickupAddress) ? "-" : r.PickupAddress);
                    card.OrderDateLine = "Дата заказа: " + (r.OrderDate.HasValue ? r.OrderDate.Value.ToString("dd.MM.yyyy") : "-");
                    card.DeliveryDateText = "Дата выдачи: " + (r.DeliveryDate.HasValue ? r.DeliveryDate.Value.ToString("dd.MM.yyyy") : "-");

                    list.Add(card);
                }
            }

            _allOrders = list.ToArray();
            LvOrders.ItemsSource = _allOrders;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Role != "Администратор")
            {
                UiHelper.ShowWarning("Добавление заказов доступно только администратору.", "Запрещено");
                return;
            }

            var win = new OrderEditWindow(null);
            win.Owner = this;

            bool? result = win.ShowDialog();
            if (result == true)
                LoadOrders();
        }

        private void LvOrders_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UserSession.Role != "Администратор")
                return;

            var selected = LvOrders.SelectedItem as OrderCardForList;
            if (selected == null) return;

            var win = new OrderEditWindow(selected.OrderID);
            win.Owner = this;

            bool? result = win.ShowDialog();
            if (result == true)
                LoadOrders();
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Role != "Администратор")
            {
                UiHelper.ShowWarning("Удаление заказов доступно только администратору.", "Запрещено");
                return;
            }

            var selected = LvOrders.SelectedItem as OrderCardForList;
            if (selected == null)
            {
                UiHelper.ShowWarning("Выберите заказ в списке.", "Нет выбора");
                return;
            }

            bool ok = UiHelper.AskYesNo("Удалить выбранный заказ?\nОперация необратима.", "Удаление заказа");
            if (!ok) return;

            try
            {
                using (var db = new Shoes_Melnikovv_522Entitiess())
                {
                    // Сначала удаляем детали заказа (FK)
                    var details = db.OrderDetails.Where(d => d.OrderID == selected.OrderID).ToList();
                    if (details.Count > 0)
                        db.OrderDetails.RemoveRange(details);

                    var order = db.Orders.FirstOrDefault(o => o.OrderID == selected.OrderID);
                    if (order == null)
                    {
                        UiHelper.ShowWarning("Заказ не найден. Обновите список.", "Ошибка данных");
                        return;
                    }

                    db.Orders.Remove(order);
                    db.SaveChanges();
                }

                UiHelper.ShowInfo("Заказ удалён.", "Готово");
                LoadOrders();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(UiHelper.BuildExceptionText("Не удалось удалить заказ.", ex));
            }
        }

        // DTO для SqlQuery
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
