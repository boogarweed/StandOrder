using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StandOrder.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public int? ItemNumber { get; set; }

        [StringLength(255)]
        public string? ProductName { get; set; }

        [StringLength(20)]
        public string? WarehouseLocation { get; set; }
        public int? PacksPerCase { get; set; }
        public int? ItemsPerPack { get; set; }

        [StringLength(1)] // 'char' in SQL typically maps to string of length 1 in C#
        public string? ProductYear { get; set; }

        [StringLength(255)] // Adjust as needed
        public string? UniversalItemNumber { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? QtyInStock { get; set; }

        public int? QuantityInStock { get; set; }

        [StringLength(32)]
        public string? Dotexnum { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CaseWeightKilos { get; set; }

        [StringLength(10)]
        public string? PackName { get; set; }

        [StringLength(10)]
        public string? ItemPackName { get; set; }

        // 'Y' / 'N'
        [StringLength(1)]
        public string? NetItem { get; set; }

        [StringLength(20)]
        public string? ProductSource { get; set; }

        [StringLength(60)]
        public string? BarCode { get; set; }

        // nvarchar(MAX)
        public string? ProductDescription { get; set; }

        [Column(TypeName = "money")] public decimal? UnitPrice1 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice2 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice3 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice4 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice5 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice6 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice7 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice8 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice9 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice10 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice11 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice12 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice13 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice14 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice15 { get; set; }
        [Column(TypeName = "money")] public decimal? UnitPrice16 { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public bool? InStock { get; set; }

        [NotMapped]
        public string DisplayName => $"{ItemNumber} - {ProductName}";

    }
}
