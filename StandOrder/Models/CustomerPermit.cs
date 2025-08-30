using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class CustomerPermit
{
    public int CustomerPermitsId { get; set; }

    public int CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? BusinessName { get; set; }

    public string? BusinessAddress { get; set; }

    public string? BusinessCity { get; set; }

    public string? BusinessState { get; set; }

    public string? IndustryCode { get; set; }

    public string? CityCode { get; set; }

    public DateOnly? SiteEffectiveDate { get; set; }

    public DateOnly ExpirationDate { get; set; }

    public string SalesAccountId { get; set; } = null!;

    public string SalesPermitNumber { get; set; } = null!;

    public string? NotifyYear { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
