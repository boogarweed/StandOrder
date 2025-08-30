using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class WebOrder
{
    public int OrderId { get; set; }

    public DateTime DateTime { get; set; }

    public string LastName { get; set; } = null!;

    public int? CustomerId { get; set; }

    public string? EmailAddress { get; set; }

    public int ItemNumber { get; set; }

    public decimal Qty { get; set; }

    public string? CustVerified { get; set; }

    public string? TypeOrder { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PickupTime { get; set; }

    public string? Pulled { get; set; }

    public string? Puller { get; set; }

    public int? NumCases { get; set; }

    public string? Location { get; set; }

    public decimal? QtyPulled { get; set; }
}
