// /Data/Customer.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace StandOrder.Models
{
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [StringLength(30)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(255)]
        public string? BillingAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? StateOrProvince { get; set; }

        [StringLength(20)]
        public string? ZIPCode { get; set; }

        [StringLength(75)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(30)]
        public string? CellPhoneNumber { get; set; }

        [StringLength(30)]
        public string? FaxNumber { get; set; }

        [StringLength(255)]
        public string? ShipAddress { get; set; }

        [StringLength(50)]
        public string? ShipCity { get; set; }

        [StringLength(50)]
        public string? ShipStateOrProvince { get; set; }

        [StringLength(20)]
        public string? ShipZIPCode { get; set; }

        [StringLength(30)]
        public string? ShipPhoneNumber { get; set; }

        // nvarchar(MAX)
        public string? Notes { get; set; }

        // Computed in the database as "[LastName], [FirstName]" (see AppDbContext).
        // Read-only: never written on insert/update.
        [StringLength(82)]
        public string? FullName { get; set; }

        public DateTime? DateAdded { get; set; }

        // 'Y' / 'N'
        [StringLength(1)]
        public string? SendMail { get; set; }

        [StringLength(4)]
        public string? LastInvoiceYear { get; set; }

        [StringLength(25)]
        public string? LastPriceSheet { get; set; }

        // 'Y' / 'N'
        [StringLength(1)]
        public string? CompanyLocation { get; set; }

        [StringLength(200)]
        public string? BusinessName { get; set; }

        // 'Y' / 'N' — not on the legacy frmUpdateCustomers screen; mapped for completeness.
        [StringLength(1)]
        public string? WholesaleCustomer { get; set; }

        public bool? Email_Marketing { get; set; }

        public bool? Email_Order_Updates { get; set; }

        public bool? Email_Pickup_Reminders { get; set; }

        // Navigation: all orders placed by this customer
        public virtual ICollection<OrdersTaken> OrdersTaken { get; set; } = new List<OrdersTaken>();
    }
}
