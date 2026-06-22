using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StandOrder.Models
{
    // Mirrors dbo.SupplierPriceSheets (the yearly supplier price snapshot).
    [Table("SupplierPriceSheets")]
    public class SupplierPriceSheet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierPriceSheetID { get; set; }

        public int SupplierID { get; set; }

        [Column(TypeName = "nchar(4)")]
        public string ProductYear { get; set; } = "";

        [StringLength(255)]
        public string? ItemName { get; set; }

        [StringLength(255)]
        public string? Category { get; set; }

        [StringLength(50)]
        public string? Packing { get; set; }

        [StringLength(50)]
        public string? SIN { get; set; }

        [Column(TypeName = "money")]
        public decimal? CasePrice { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(255)]
        public string? SourceFile { get; set; }

        public DateTime ImportedAt { get; set; }

        // NULL = earliest loaded year (no prior to compare); 1 = new this year; 0 = returning
        public bool? NewItem { get; set; }

        // Navigation
        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }
    }
}
