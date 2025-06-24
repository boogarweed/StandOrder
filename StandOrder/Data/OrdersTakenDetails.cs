using System.ComponentModel.DataAnnotations;

namespace StandOrder.Data
{
    public class OrdersTakenDetails
    {
        public int OrdersTakenDetailsID { get; set; }

        [Required]
        public int OrdersTakenID { get; set; }

        [Required]
        public OrdersTaken OrdersTaken { get; set; }

        [Required]
        public int ProductID { get; set; }
        public int QuantityOrdered { get; set; }
        public int? QuantityPulled { get; set; }
        public int? QuantityLoaded { get; set; }
        public string Notes { get; set; }
    }
}
