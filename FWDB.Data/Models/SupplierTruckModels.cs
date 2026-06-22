using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using StandOrder.Models;

namespace StandOrder.Models
{
    // Supplier entity
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierID { get; set; }

        [Required, StringLength(255)]
        public string SupplierName { get; set; }

        // Navigation: Trucks received from this supplier
        public virtual ICollection<Truck> Trucks { get; set; } = new List<Truck>();
    }

    // Truck entity
    public class Truck
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TruckID { get; set; }

        [ForeignKey("Supplier")]
        [Required]
        public int SupplierID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ReceivedDate { get; set; }

        // Navigation
        public virtual Supplier Supplier { get; set; }
        public virtual ICollection<TruckProducts> TruckProducts { get; set; } = new List<TruckProducts>();
    }

    // TruckProducts entity
    public class TruckProducts
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TruckProductID { get; set; }

        [ForeignKey("Truck")]
        [Required]
        public int TruckID { get; set; }

        [ForeignKey("Product")]
        [Required]
        public int ProductID { get; set; }

        [Required]
        public int QuantityReceived { get; set; }

        // Navigation
        public virtual Truck Truck { get; set; }
        public virtual Product Product { get; set; }
    }

    //// DbContext extension
    //public partial class AppDbContext : DbContext
    //{
    //    public DbSet<Supplier> Suppliers { get; set; }
    //    public DbSet<Truck> Trucks { get; set; }
    //    public DbSet<TruckProducts> TruckProducts { get; set; }
    //}
}
