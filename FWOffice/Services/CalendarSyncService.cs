using Microsoft.EntityFrameworkCore;
using StandOrder.Models;

namespace FWOffice.Services
{
    // Keeps the calendar (Appointments) in sync with an order's pickup time.
    //
    // Rule: every OrdersTaken with a non-null PickUpTime owns exactly one Appointment row
    // (linked by Appointments.OrdersTakenID). Saving an order upserts that appointment;
    // clearing the pickup removes it. Deleting the order removes the appointment automatically
    // via the ON DELETE CASCADE foreign key, so no delete handling is needed here.
    // Manually-created appointments (OrdersTakenID NULL) are never touched.
    public class CalendarSyncService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CalendarSyncService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public async Task SyncOrderPickupAsync(int ordersTakenId)
        {
            await using var db = _factory.CreateDbContext();

            var order = await db.OrdersTakens.FirstOrDefaultAsync(o => o.OrdersTakenID == ordersTakenId);
            if (order == null) return;

            var appt = await db.Appointments.FirstOrDefaultAsync(a => a.OrdersTakenID == ordersTakenId);

            // No pickup time -> there should be no appointment.
            if (order.PickUpTime == null)
            {
                if (appt != null) { db.Appointments.Remove(appt); await db.SaveChangesAsync(); }
                return;
            }

            var custName = await db.Customers.Where(c => c.CustomerID == order.CustomerID)
                .Select(c => c.FullName).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(custName))
                custName = string.IsNullOrWhiteSpace(order.LastName) ? $"Order #{ordersTakenId}" : order.LastName;

            var itemCount = await db.OrdersTakenDetails.CountAsync(d => d.OrdersTakenID == ordersTakenId);
            var notes = $"Order #{ordersTakenId} pickup · {order.TypeOrder} · {itemCount} item(s)";

            if (appt == null)
            {
                db.Appointments.Add(new Appointment
                {
                    OrdersTakenID = ordersTakenId,
                    CustomerName = custName,
                    AppointmentDate = order.PickUpTime,
                    Notes = notes
                });
            }
            else
            {
                appt.CustomerName = custName;
                appt.AppointmentDate = order.PickUpTime;
                appt.Notes = notes;
            }
            await db.SaveChangesAsync();
        }
    }
}
