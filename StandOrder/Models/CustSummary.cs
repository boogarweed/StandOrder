using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class CustSummary
{
    public int CustomerId { get; set; }

    public string? FullName { get; set; }

    public int ItemNumber { get; set; }

    public string? ProductName { get; set; }

    public double? Quantity { get; set; }

    public string? ProductYear { get; set; }

    public string? CaseQty { get; set; }

    public int? PacksPerCase { get; set; }

    public string? PackName { get; set; }
}
