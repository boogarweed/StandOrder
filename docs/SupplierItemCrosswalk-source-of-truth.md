# dbo.SupplierItemCrosswalk — source of truth

`dbo.SupplierItemCrosswalk` is the authoritative mapping between **our Universal Item Numbers
(UIN)** and each **supplier's item number (SIN)**. It drives pricing, so it must stay accurate.

## What changed

Historically this table was populated by a generated SQL script that **DROPs and recreates**
the table from a spreadsheet export. That was a one-way load: the spreadsheet was the source of
truth and the database was a disposable copy.

The FWOffice screen **Products → Supplier Crosswalk** (`/products/crosswalk`,
`FWOffice/Components/Pages/SupplierCrosswalk.razor`) now lets the product manager edit this table
**directly** — add, change, reassign, and remove UIN↔SIN mappings.

## The rule

**The database is now the source of truth. Do NOT re-run the spreadsheet drop/recreate reload
script.** Running it will silently wipe every edit made through the screen.

If you ever need to bulk-load from a spreadsheet again:

- Do **not** DROP/TRUNCATE the table.
- Merge instead (e.g. `MERGE` on the unique key `(SupplierID, SupplierItemNumber)`), and decide
  deliberately whether the spreadsheet or the existing rows win on conflict.
- Back up the table first: `SELECT * INTO SupplierItemCrosswalk_backup_yyyymmdd FROM SupplierItemCrosswalk;`

## Business rules enforced by the screen

- A SIN maps to exactly one UIN — `(SupplierID, SupplierItemNumber)` is unique. Picking a supplier
  item already mapped to a different UIN prompts to **reassign (move)** it rather than failing.
- A UIN may have many SINs (typically one per supplier).
- SINs on the same UIN should share the same Packing; a mismatch shows a warning (not a hard block).
- SINs are normalized to trimmed strings (numeric SINs stored as `1172`, not `1172.0`).
