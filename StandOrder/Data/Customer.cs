// /Data/Customer.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace StandOrder.Data
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required, StringLength(255)]
        public string? FullName { get; set; }

        // Navigation: all orders placed by this customer
        public virtual ICollection<OrdersTaken> OrdersTaken { get; set; } = new List<OrdersTaken>();
    }
}
