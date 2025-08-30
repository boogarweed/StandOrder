using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int CustomerId { get; set; }

    public decimal? PayAmount { get; set; }

    public DateOnly? PayDate { get; set; }

    public string? PayType { get; set; }

    public bool? PayHold { get; set; }

    public string? CheckNum { get; set; }

    public string? PayYear { get; set; }

    public bool? JoesStand { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
