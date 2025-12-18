using System;

namespace up_melnilov_522.Models
{
    public class OrderCardForList
    {
        public int OrderID { get; set; }

        public string ArticleLine { get; set; }         // "Артикул заказа: 1"
        public string StatusLine { get; set; }          // "Статус заказа: Новый"
        public string PickupAddressLine { get; set; }   // "Адрес пункта выдачи: ..."
        public string OrderDateLine { get; set; }       // "Дата заказа: ..."
        public string DeliveryDateText { get; set; }    // "Дата выдачи: ..."

        // Для редактирования
        public string OrderStatus { get; set; }
        public string PickupAddress { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
}
