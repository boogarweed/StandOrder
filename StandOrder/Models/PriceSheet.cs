using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class PriceSheet
{
    public int PriceId { get; set; }

    public string PriceSheetNumber { get; set; } = null!;

    public string PriceSheetName { get; set; } = null!;

    public string? CaseQty { get; set; }
}
