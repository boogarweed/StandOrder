using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public float? Discount { get; set; }

    public string? Comment { get; set; }

    public string? PriceSheetNumber { get; set; }

    public decimal? OrderTotal { get; set; }

    public decimal? StandCharges { get; set; }

    public decimal? LicenseCharges { get; set; }

    public decimal? InsuranceCharges { get; set; }

    public decimal? MiscCharges { get; set; }

    public decimal? MiscCredit { get; set; }

    public decimal? NetTotal { get; set; }

    public DateTime? LastUpdateDate { get; set; }

    public string? ChargeText { get; set; }

    public string? CreditText { get; set; }

    public string? OrderYear { get; set; }

    public string? LastUpdateUser { get; set; }

    public int? OrdersTakenId { get; set; }

    public decimal? SalesTax { get; set; }

    public string? Invoice { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
