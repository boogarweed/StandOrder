using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class ProductStockLog
{
    public int LogId { get; set; }

    public int ProductId { get; set; }

    public int? OldQuantity { get; set; }

    public int? NewQuantity { get; set; }

    public int? ChangeAmount { get; set; }

    public DateTime? LogTimestamp { get; set; }

    public string? UpdatedBy { get; set; }

    public string? Source { get; set; }

    public string? SourceParameters { get; set; }
}
