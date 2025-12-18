using System;

namespace up_melnilov_522.Models
{
    public class OrderView
    {
        public int OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string OrderStatus { get; set; }
        public string UserFullName { get; set; }
        public int? UserID { get; set; }
    }
}
