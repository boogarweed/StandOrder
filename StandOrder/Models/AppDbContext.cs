using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StandOrder.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<Barcode> Barcodes { get; set; }

    public virtual DbSet<CustSummary> CustSummaries { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerPermit> CustomerPermits { get; set; }

    public virtual DbSet<MigrationHistory> MigrationHistories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrdersTaken> OrdersTakens { get; set; }

    public virtual DbSet<OrdersTakenDetails> OrdersTakenDetails { get; set; }

    public virtual DbSet<OurCompanyInfo> OurCompanyInfos { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentType> PaymentTypes { get; set; }

    public virtual DbSet<PriceSheet> PriceSheets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductStockLog> ProductStockLogs { get; set; }

    public virtual DbSet<Prodweight> Prodweights { get; set; }

    public virtual DbSet<RetailBarcode> RetailBarcodes { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TriggerLog> TriggerLogs { get; set; }

    public virtual DbSet<Truck> Trucks { get; set; }

    public virtual DbSet<TruckProducts> TruckProducts { get; set; }

    public virtual DbSet<WebOrder> WebOrders { get; set; }

    public virtual DbSet<YesNo> YesNos { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=localhost\\SQLExpress;Database=Fireworks;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCA2A4A2F11D");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentID");
            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetRoles");

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetUsers");

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.LockoutEndDateUtc).HasColumnType("datetime");
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK_dbo.AspNetUserRoles");
                        j.ToTable("AspNetUserRoles");
                        j.IndexerProperty<string>("UserId").HasMaxLength(128);
                        j.IndexerProperty<string>("RoleId").HasMaxLength(128);
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetUserClaims");

            entity.Property(e => e.UserId).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId");
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey, e.UserId }).HasName("PK_dbo.AspNetUserLogins");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);
            entity.Property(e => e.UserId).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId");
        });

        modelBuilder.Entity<Barcode>(entity =>
        {
            entity.Property(e => e.BarCodeID).HasColumnName("BarCodeID");
            entity.Property(e => e.BarCodeType).HasColumnName("BarCodeType");
            entity.Property(e => e.UniversalItemNumber).HasColumnName("UniversalItemNumber");
            entity.Property(e => e.BarCode).HasColumnName("BarCode");

        });

        modelBuilder.Entity<CustSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("CustSummary");

            entity.Property(e => e.CaseQty)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.FullName).HasMaxLength(82);
            entity.Property(e => e.PackName).HasMaxLength(10);
            entity.Property(e => e.ProductName).HasMaxLength(50);
            entity.Property(e => e.ProductYear)
                .HasMaxLength(4)
                .IsFixedLength();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.CustomerID).HasColumnName("CustomerID");
            entity.Property(e => e.FullName)
                .HasMaxLength(82)
                .HasComputedColumnSql("(([LastName]+', ')+[FirstName])", true);
        });

        modelBuilder.Entity<CustomerPermit>(entity =>
        {
            entity.HasKey(e => e.CustomerPermitsId);

            entity.Property(e => e.CustomerPermitsId).HasColumnName("CustomerPermitsID");
            entity.Property(e => e.BusinessAddress).HasMaxLength(255);
            entity.Property(e => e.BusinessCity).HasMaxLength(50);
            entity.Property(e => e.BusinessName).HasMaxLength(82);
            entity.Property(e => e.BusinessState)
                .HasMaxLength(2)
                .IsFixedLength();
            entity.Property(e => e.CityCode)
                .HasMaxLength(4)
                .IsFixedLength();
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(82);
            entity.Property(e => e.IndustryCode)
                .HasMaxLength(6)
                .IsFixedLength();
            entity.Property(e => e.NotifyYear)
                .HasMaxLength(4)
                .IsFixedLength();
            entity.Property(e => e.SalesAccountId)
                .HasMaxLength(50)
                .HasColumnName("SalesAccountID");
            entity.Property(e => e.SalesPermitNumber).HasMaxLength(50);

        });

        modelBuilder.Entity<MigrationHistory>(entity =>
        {
            entity.HasKey(e => new { e.MigrationId, e.ContextKey }).HasName("PK_dbo.__MigrationHistory");

            entity.ToTable("__MigrationHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ContextKey).HasMaxLength(300);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.CustomerId, "IX_Orders");

            entity.HasIndex(e => e.OrderYear, "IX_Orders_1");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ChargeText).HasMaxLength(50);
            entity.Property(e => e.CreditText).HasMaxLength(50);
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.InsuranceCharges).HasColumnType("money");
            entity.Property(e => e.LastUpdateDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(150);
            entity.Property(e => e.LicenseCharges).HasColumnType("money");
            entity.Property(e => e.MiscCharges).HasColumnType("money");
            entity.Property(e => e.MiscCredit).HasColumnType("money");
            entity.Property(e => e.NetTotal).HasColumnType("money");
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.OrderTotal).HasColumnType("money");
            entity.Property(e => e.OrderYear)
                .HasMaxLength(4)
                .IsFixedLength();
            entity.Property(e => e.OrdersTakenId).HasColumnName("OrdersTakenID");
            entity.Property(e => e.PriceSheetNumber).HasMaxLength(20);
            entity.Property(e => e.SalesTax).HasColumnType("money");
            entity.Property(e => e.StandCharges).HasColumnType("money");

        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("Order Details");

            entity.HasIndex(e => e.ProductId, "ProductIDNDX");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UnitPrice).HasColumnType("money");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order Details_Orders");

        });

        modelBuilder.Entity<OrdersTaken>(entity =>
        {
            entity.ToTable("OrdersTaken");

            entity.Property(e => e.OrdersTakenID).HasColumnName("OrdersTakenID");
            entity.Property(e => e.CustomerID).HasColumnName("CustomerID");
            entity.Property(e => e.EmailAddress).HasMaxLength(150);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(50);
            entity.Property(e => e.OrderYear)
                .HasMaxLength(4)
                .IsFixedLength();
            entity.Property(e => e.PhoneNumber).HasMaxLength(14);
            entity.Property(e => e.PickUpTime).HasColumnType("datetime");
            entity.Property(e => e.PullNow)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TimeTaken).HasColumnType("datetime");
            entity.Property(e => e.TypeOrder).HasMaxLength(7);

        });

        modelBuilder.Entity<OrdersTakenDetails>(entity =>
        {
            entity.HasKey(e => e.OrdersTakenDetailsID);

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateProductStock"));

            entity.Property(e => e.OrdersTakenDetailsID).HasColumnName("OrdersTakenDetailsID");
            entity.Property(e => e.Notes).HasMaxLength(100);
            entity.Property(e => e.OrdersTakenID).HasColumnName("OrdersTakenID");
            entity.Property(e => e.ProductID).HasColumnName("ProductID");

            entity.HasOne(d => d.OrdersTaken).WithMany(p => p.OrdersTakenDetails)
                .HasForeignKey(d => d.OrdersTakenID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdersTakenDetails_OrdersTaken");

        });

        modelBuilder.Entity<OurCompanyInfo>(entity =>
        {
            entity.HasKey(e => e.SetupId);

            entity.ToTable("Our Company Info");

            entity.Property(e => e.SetupId).HasColumnName("SetupID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.CompanyName).HasMaxLength(50);
            entity.Property(e => e.CompanyNameLine2).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(75);
            entity.Property(e => e.FaxNumber).HasMaxLength(30);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.SalesTaxRate).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.StateOrProvince).HasMaxLength(20);
            entity.Property(e => e.WebSite).HasMaxLength(50);
            entity.Property(e => e.WholesaleLicenseNumber)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Zipcode)
                .HasMaxLength(20)
                .HasColumnName("ZIPCode");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.PayYear, "IX_Payments");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.CheckNum).HasMaxLength(5);
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PayAmount).HasColumnType("money");
            entity.Property(e => e.PayType).HasMaxLength(15);
            entity.Property(e => e.PayYear)
                .HasMaxLength(4)
                .IsFixedLength();

        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.PayTypeId);

            entity.ToTable("PaymentType");

            entity.Property(e => e.PayTypeId).HasColumnName("PayTypeID");
            entity.Property(e => e.PayType).HasMaxLength(20);
        });

        modelBuilder.Entity<PriceSheet>(entity =>
        {
            entity.HasKey(e => e.PriceId);

            entity.Property(e => e.PriceId).HasColumnName("PriceID");
            entity.Property(e => e.CaseQty)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.PriceSheetName).HasMaxLength(50);
            entity.Property(e => e.PriceSheetNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_LogProductStockUpdate"));

            entity.HasIndex(e => e.ProductYear, "IX_Products");

            entity.Property(e => e.ProductID).HasColumnName("ProductID");
            //entity.Property(e => e.BarCode).HasMaxLength(60);
            //entity.Property(e => e.CaseWeightKilos).HasColumnType("decimal(18, 2)");
            //entity.Property(e => e.Category).HasMaxLength(50);
            //entity.Property(e => e.Dotexnum)
            //    .HasMaxLength(32)
            //    .HasColumnName("DOTEXNum");
            //entity.Property(e => e.ItemNumName)
            //    .HasMaxLength(59)
            //    .HasComputedColumnSql("((CONVERT([nvarchar](5),[ItemNumber],(0))+' -- ')+CONVERT([nvarchar](50),[ProductName],(0)))", false);
            //entity.Property(e => e.ItemPackName).HasMaxLength(10);
            //entity.Property(e => e.ItemPicture).HasColumnType("image");
            //entity.Property(e => e.NetItem).HasMaxLength(1);
            //entity.Property(e => e.PackName).HasMaxLength(10);
            //entity.Property(e => e.ProductName).HasMaxLength(50);
            //entity.Property(e => e.ProductSource).HasMaxLength(20);
            entity.Property(e => e.ProductYear)
                .HasMaxLength(4)
                .IsFixedLength();
            entity.Property(e => e.QtyInStock).HasColumnType("decimal(8, 2)");
            //entity.Property(e => e.UnitPrice1)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice10)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice11).HasColumnType("money");
            //entity.Property(e => e.UnitPrice12).HasColumnType("money");
            //entity.Property(e => e.UnitPrice13).HasColumnType("money");
            //entity.Property(e => e.UnitPrice14).HasColumnType("money");
            //entity.Property(e => e.UnitPrice15).HasColumnType("money");
            //entity.Property(e => e.UnitPrice16).HasColumnType("money");
            //entity.Property(e => e.UnitPrice2)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice3)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice4)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice5)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice6)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice7)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice8)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            //entity.Property(e => e.UnitPrice9)
            //    .HasDefaultValue(0m)
            //    .HasColumnType("money");
            entity.Property(e => e.UniversalItemNumber).HasMaxLength(20);
            entity.Property(e => e.WarehouseLocation).HasMaxLength(20);
        });

        modelBuilder.Entity<ProductStockLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__ProductS__5E5499A8EBA5FA10");

            entity.ToTable("ProductStockLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.LogTimestamp).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Source).HasMaxLength(128);
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(128)
                .HasDefaultValueSql("(suser_sname())");
        });

        modelBuilder.Entity<Prodweight>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("prodweight");

            entity.Property(e => e.Caseweightkilos)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("caseweightkilos");
            entity.Property(e => e.Productname)
                .HasMaxLength(50)
                .HasColumnName("productname");
        });

        modelBuilder.Entity<RetailBarcode>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Barcode).HasMaxLength(60);
            entity.Property(e => e.BarcodeType).HasMaxLength(10);
            entity.Property(e => e.UniversalItemNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.SupplierID).HasColumnName("SupplierID");
            entity.Property(e => e.SupplierName).HasMaxLength(50);
        });

        modelBuilder.Entity<TriggerLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__TriggerL__5E5499A89D88D66E");

            entity.ToTable("TriggerLog");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.LogDate).HasColumnType("datetime");
            entity.Property(e => e.LogMessage).HasMaxLength(100);
        });

        modelBuilder.Entity<Truck>()
        .HasOne(t => t.Supplier)
        .WithMany(s => s.Trucks)
        .HasForeignKey(t => t.SupplierID)
        .OnDelete(DeleteBehavior.Restrict); // optional

        modelBuilder.Entity<TruckProducts>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_UpdateProductStock_OnReceipt"));

            entity.HasOne(tp => tp.Truck)
                .WithMany(t => t.TruckProducts)
                .HasForeignKey(tp => tp.TruckID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Product)
                .WithMany()
                .HasForeignKey(tp => tp.ProductID)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<WebOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustVerified)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DateTime).HasColumnType("datetime");
            entity.Property(e => e.EmailAddress).HasMaxLength(150);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(50);
            entity.Property(e => e.NumCases).HasColumnName("Num_Cases");
            entity.Property(e => e.PhoneNumber).HasMaxLength(14);
            entity.Property(e => e.PickupTime)
                .HasMaxLength(25)
                .IsFixedLength();
            entity.Property(e => e.Pulled)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.Puller).HasMaxLength(20);
            entity.Property(e => e.Qty).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QtyPulled).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.TypeOrder).HasMaxLength(7);
        });

        modelBuilder.Entity<YesNo>(entity =>
        {
            entity.ToTable("YesNo");

            entity.Property(e => e.YesNoId).HasColumnName("YesNoID");
            entity.Property(e => e.YesNo1)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("YesNo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
