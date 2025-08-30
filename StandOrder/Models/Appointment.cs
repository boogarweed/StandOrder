using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public string? CustomerName { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public string? Notes { get; set; }
}
