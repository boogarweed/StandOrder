using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class PaymentType
{
    public int PayTypeId { get; set; }

    public string PayType { get; set; } = null!;
}
