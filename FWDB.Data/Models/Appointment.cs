using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public string? CustomerName { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public string? Notes { get; set; }

    // Set when this appointment is the auto-synced pickup for an order (see CalendarSyncService).
    // NULL for manually-created calendar appointments. FK to OrdersTaken with ON DELETE CASCADE.
    public int? OrdersTakenID { get; set; }
}
