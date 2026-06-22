//using System;
//using System.Collections.Generic;

//namespace StandOrder.Models;

//public partial class Product
//{
//    public int ProductId { get; set; }

//    public int ItemNumber { get; set; }

//    public string? ProductName { get; set; }

//    public bool? InStock { get; set; }

//    public int? QuantityInStock { get; set; }

//    public decimal? UnitPrice1 { get; set; }

//    public decimal? UnitPrice2 { get; set; }

//    public decimal? UnitPrice3 { get; set; }

//    public decimal? UnitPrice4 { get; set; }

//    public decimal? UnitPrice5 { get; set; }

//    public decimal? UnitPrice6 { get; set; }

//    public decimal? UnitPrice7 { get; set; }

//    public decimal? UnitPrice8 { get; set; }

//    public decimal? UnitPrice9 { get; set; }

//    public decimal? UnitPrice10 { get; set; }

//    public byte[]? ItemPicture { get; set; }

//    public string? NetItem { get; set; }

//    public string? PackName { get; set; }

//    public int? ItemsPerPack { get; set; }

//    public int? PacksPerCase { get; set; }

//    public string? Dotexnum { get; set; }

//    public decimal? UnitPrice11 { get; set; }

//    public decimal? UnitPrice12 { get; set; }

//    public decimal? UnitPrice13 { get; set; }

//    public decimal? UnitPrice14 { get; set; }

//    public decimal? UnitPrice15 { get; set; }

//    public string? ProductSource { get; set; }

//    public string? ItemPackName { get; set; }

//    public string? ProductYear { get; set; }

//    public decimal? UnitPrice16 { get; set; }

//    public string? BarCode { get; set; }

//    public string? WarehouseLocation { get; set; }

//    public decimal? QtyInStock { get; set; }

//    public string? Category { get; set; }

//    public decimal? CaseWeightKilos { get; set; }

//    public string? ItemNumName { get; set; }

//    public string? UniversalItemNumber { get; set; }

//    public string? ProductDescription { get; set; }

//    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

//    public virtual ICollection<OrdersTakenDetail> OrdersTakenDetails { get; set; } = new List<OrdersTakenDetail>();

//    public virtual ICollection<ProductsReceived> ProductsReceiveds { get; set; } = new List<ProductsReceived>();
//}
