using System.Windows.Media.Imaging;

namespace up_melnilov_522.Models
{
    public class ProductCardForList
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        // Для отображения строками
        public string ManufacturerLine { get; set; }
        public string SupplierLine { get; set; }

        public string UnitLine { get; set; }
        public string StockLine { get; set; }

        public string DiscountLine { get; set; }

        // Для логики
        public int Discount { get; set; }
        public int StockQuantity { get; set; }

        public bool HasDiscount { get; set; }
        public string OldPriceText { get; set; }
        public string PriceText { get; set; }

        public string SupplierNameRaw { get; set; }
        public string ManufacturerNameRaw { get; set; }
        public string UnitOfMeasureRaw { get; set; }
        public string PhotoPathRaw { get; set; }

        public BitmapImage PhotoImage { get; set; }
    }
}
