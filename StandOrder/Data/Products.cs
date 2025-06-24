using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StandOrder.Data
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        public int? ItemNumber { get; set; }

        [StringLength(255)] // Assuming a reasonable max length for varchar, adjust as needed
        public string? ProductName { get; set; }

        [StringLength(1)] // 'char' in SQL typically maps to string of length 1 in C#
        public string? ProductYear { get; set; }

        [StringLength(255)] // Adjust as needed
        public string? UniversalItemNumber { get; set; }
        
        [Column(TypeName = "decimal(8,2)")]
        public decimal? QtyInStock { get; set; }


    }
}
