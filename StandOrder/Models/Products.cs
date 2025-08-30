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

        [Column(TypeName = "decimal(8,2)")]
        public decimal? QtyInStock { get; set; }

        public int? QuantityInStock { get; set; }


    }
}
