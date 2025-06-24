using System.ComponentModel.DataAnnotations;

namespace StandOrder.Data
{
    public class OrdersTaken
    {
        public int OrdersTakenID { get; set; }

        [Required]
        public DateTime TimeTaken { get; set; }

        [Required]
        public int CustomerID { get; set; }
        public string EmailAddress { get; set; }

        [Required]
        public string TypeOrder { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? PickUpTime { get; set; }

        [Required]
        public string Status { get; set; }
        public string PullerID { get; set; }
        public int? Num_Cases { get; set; }
        public string Location { get; set; }

        [Required]
        public string OrderYear { get; set; }
        public string LastName { get; set; }
        public string PullNow { get; set; }
        public List<OrdersTakenDetails> OrdersTakenDetails { get; set; }
    }
}
