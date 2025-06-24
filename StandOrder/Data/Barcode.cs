using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StandOrder.Data
{
    public class Barcode
    {
        [Key]
        public int BarCodeID { get; set; }

        public string? UniversalItemNumber { get; set; }

        [StringLength(50)] // Assuming a reasonable max length for varchar, adjust as needed
        public string? BarCodeType { get; set; }

        [StringLength(255)] // Assuming a reasonable max length for varchar, adjust as needed
        public string? BarCode { get; set; } // Renamed to avoid conflict with class name

        // Navigation property
        public virtual Product Product { get; set; }
    }
}
