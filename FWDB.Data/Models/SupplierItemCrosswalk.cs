using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StandOrder.Models
{
    [Table("SupplierItemCrosswalk")]
    public class SupplierItemCrosswalk
    {
        [Key]
        public int CrosswalkID { get; set; }

        public int SupplierID { get; set; }

        public string? SupplierItemNumber { get; set; }

        public string? UniversalItemNumber { get; set; }

        public string? SupplierProductName { get; set; }

        public string? Category { get; set; }

        public string? Packing { get; set; }
    }
}
