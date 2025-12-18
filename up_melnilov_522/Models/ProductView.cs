namespace up_melnilov_522.Models
{
    public class ProductView
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string ManufacturerName { get; set; }
        public string SupplierName { get; set; }

        public decimal UnitPrice { get; set; }
        public int Discount { get; set; }
        public decimal FinalPrice { get; set; }

        public string UnitOfMeasure { get; set; }
        public int StockQuantity { get; set; }

        public string Photo { get; set; } // имя файла/путь как в БД
    }
}
