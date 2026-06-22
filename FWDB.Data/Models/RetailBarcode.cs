using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class RetailBarcode
{
    public string UniversalItemNumber { get; set; } = null!;

    public string BarcodeType { get; set; } = null!;

    public string? Barcode { get; set; }
}
