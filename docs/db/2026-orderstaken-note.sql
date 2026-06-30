-- Adds an order-level note to OrdersTaken.
-- Applied to the Fireworks DB on 2026-06-30. Idempotent. (This repo uses scaffolded entities,
-- not EF migrations, so schema changes are tracked as scripts here — run this on production
-- before deploying the matching FWOffice/StandOrder build.)
--
-- The note is edited only on the FWOffice order editor and displayed (read-only) on the
-- StandOrder pull screen. It is intentionally NULLABLE so it stays invisible to the external
-- OrdersTaken API (which uses a hand-mapped EF entity that doesn't include this column, and
-- whose named-column inserts/selects ignore it). Do NOT make it NOT NULL.

IF COL_LENGTH('dbo.OrdersTaken', 'Note') IS NULL
    ALTER TABLE dbo.OrdersTaken ADD Note NVARCHAR(MAX) NULL;
GO
