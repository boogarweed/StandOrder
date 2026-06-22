using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class TriggerLog
{
    public int LogId { get; set; }

    public string? LogMessage { get; set; }

    public DateTime? LogDate { get; set; }
}
