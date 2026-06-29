-- Links calendar appointments to orders so an order's pickup time can sync to the calendar.
-- Applied to the Fireworks DB on 2026-06-29. Idempotent. (This repo uses scaffolded entities,
-- not EF migrations, so schema changes are tracked as scripts here.)
--
-- Used by FWOffice CalendarSyncService: each OrdersTaken with a PickUpTime owns one Appointment
-- (Appointments.OrdersTakenID). ON DELETE CASCADE removes the appointment when the order is deleted.

IF COL_LENGTH('dbo.Appointments', 'OrdersTakenID') IS NULL
    ALTER TABLE dbo.Appointments ADD OrdersTakenID INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Appointments_OrdersTaken')
    ALTER TABLE dbo.Appointments WITH CHECK
        ADD CONSTRAINT FK_Appointments_OrdersTaken
        FOREIGN KEY (OrdersTakenID) REFERENCES dbo.OrdersTaken(OrdersTakenID) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointments_OrdersTakenID')
    CREATE INDEX IX_Appointments_OrdersTakenID ON dbo.Appointments(OrdersTakenID);
GO
