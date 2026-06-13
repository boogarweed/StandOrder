# Build Spec — Supplier Price Sheet Load (StandOrder)

*For the StandOrder Blazor app. Audience: developer. Goal: a non-technical staff member picks
a supplier, picks the supplier's spreadsheet from disk, clicks **Load**, and the year's prices
land in `dbo.SupplierPriceSheets` — no CSVs, no SSMS, no manual steps.*

## 1. Scope

**In scope (this spec):** the "Load Supplier Price Sheet" page — read the supplier's Excel
file, parse it per that supplier's layout, load into `SupplierPriceSheets`, set `NewItem`,
show a result summary.

**Out of scope (separate work):** the crosswalk matching/maintenance tool, new-item review,
and cost-basis generation. Those run against data this page produces.

## 2. Fit with the existing codebase

Observed conventions to follow:

- **Blazor Server**, interactive server components (`Program.cs` → `AddInteractiveServerComponents`).
- Pages live in `Components/Pages/*.razor` with an `@page` route; nav in `Components/Layout/NavMenu.razor`.
- **Data access:** `@inject IDbContextFactory<AppDbContext> DbFactory` then
  `await using var db = DbFactory.CreateDbContext();` (the pattern in `CustomerOrder.razor`,
  `MainLayout.razor`). `AppDbContext` (namespace `StandOrder.Models`) already has
  `DbSet<Supplier> Suppliers` and `DbSet<Product> Products`.
- Connection string: `DefaultConnection` → the `Fireworks` DB.
- Service classes live in `Services/` (e.g. `TruckDataService`) and are DI-registered in `Program.cs`.

## 3. Dependencies to add

The project has **no Excel library**. Add one that reads legacy `.xls` **and** `.xlsx`
(Hales sends `.xls`, which ClosedXML/EPPlus cannot read):

```xml
<PackageReference Include="ExcelDataReader" Version="3.7.0" />
<PackageReference Include="ExcelDataReader.DataSet" Version="3.7.0" />
```

`.xls` parsing requires registering the code-page provider once at startup (in `Program.cs`):

```csharp
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```

## 4. Database objects (already defined — see Pricesheets plan)

These exist/are created on the SQL side; the app uses them:

- `dbo.SupplierPriceSheets` — target table (`SupplierPriceSheetID, SupplierID, ProductYear,
  ItemName, Category, Packing, SIN, CasePrice, Brand, SourceFile, ImportedAt, NewItem`).
- `dbo.SupplierPriceSheets_Staging` — loose loading table, same columns minus identity.
- `dbo.usp_ImportSupplierPriceSheets` — drains staging into the target (idempotent replace by
  `SupplierID+ProductYear`), recomputes `NewItem`, returns a per-supplier/year summary result set.

**EF:** add a `SupplierPriceSheet` entity + `DbSet` for reads (the result grid). The staging
load uses `SqlBulkCopy` (raw ADO via the same connection string), not EF — fast and simple.

## 5. The page (`Components/Pages/LoadPriceSheet.razor`)

Route: `@page "/load-pricesheet"`. Add a NavMenu link "Load Price Sheet".

UI:

```
Supplier:  [ dropdown ▼ ]      (from Suppliers, ordered by name)
Year:      [ 2026 ]            (defaults to current year)
File:      [ Choose file ]     (<InputFile>, accept .xls,.xlsx)
           [ Load ]            (disabled until supplier + file chosen)

── after Load ───────────────────────────────────────────────
✔ FOA 2026 loaded: 781 items  (42 new, 739 returning)
   [details table: ItemName | Category | Packing | SIN | CasePrice | New?]
   ⚠ 14 rows skipped (no price)         (only if any)
```

- File upload uses Blazor `<InputFile OnChange="...">` → `IBrowserFile`; copy to a temp file or
  `MemoryStream` (set `maxAllowedSize` ~10 MB; these files are <500 KB).
- Run parse+load in the click handler with a busy spinner; show the summary on completion.
- On any validation failure, show the error and load **nothing** (the import is transactional).

## 6. Parsing — the core logic

This is a direct port of the validated Python extractor. Open the file with ExcelDataReader,
read the configured sheet as raw rows (no header), and walk rows from `DataStartRow`.

Per-supplier config (drives one generic parser):

```csharp
record SupplierLayout(
    int     SupplierId,
    string  SheetName,        // exact worksheet name
    int     DataStartRow,     // 0-based first data row
    int     SinCol,           // column indexes (0-based)
    int     NameCol,
    int     PackingCol,
    int     CasePriceCol,
    int?    BrandCol,         // null if none
    bool    Banner,           // category carried from section-header rows
    bool    FixExcelDates,    // repair packing coerced to dates
    RepeatHeader? RepeatHdr); // Hales only

record RepeatHeader(int LabelCol, string LabelValue, int[] GroupCols, string[] SkipLabels);
```

| Supplier | SupId | Sheet | DataStart | SIN | Name | Pack | Price | Brand | Banner | FixDates | RepeatHdr |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Black Market | 1 | `PriceList` | 3 | 0 | 1 | 2 | 3 | – | yes | no | – |
| FOA | 2 | `Sheet1` | 8 | 1 | 2 | 3 | 6 | – | yes | no | – |
| Hales (.xls) | 4 | `Sheet1` | 9 | 0 | 2 | 3 | 4 | – | yes | no | **yes** |
| Red Rhino | 5 | `Sheet1` | 1 | 0 | 2 | 5 | 6 | 3 | no | **yes** | – |
| SO76 | 10 | `Price List` | 1 | 0 | 1 | 2 | 6 | 3 | yes | no | – |
| Winco | 7 | `2026 SPRING X` | 8 | 0 | 3 | 5 | 8 | 2 | yes | no | – |

Hales `RepeatHeader`: `LabelCol=0, LabelValue="Item #", GroupCols=[2,1],
SkipLabels=["QTY","PACKING","PRICE","TOTAL"]`.

### Parser algorithm (per row, from `DataStartRow`)

1. **Repeat-header (Hales):** if `RepeatHdr` set and `cell[LabelCol]` equals `LabelValue`
   (case-insensitive), set `currentCategory` = first non-empty `GroupCols` value that isn't a
   `SkipLabel`; skip the row.
2. **Parse price** from `CasePriceCol`: strip `$`, commas, spaces → decimal.
3. **Banner / section header:** if price is not a valid number:
   - if `Banner` and `cell[0]` has text that isn't a footer phrase (`subject to change`,
     `prices`, `total`), set `currentCategory = cell[0]`; then skip the row regardless.
4. **Require a SIN** (`NormalizeSin(cell[SinCol])`); skip if empty.
5. **Emit a row:** SupplierId, ProductYear (from the page), ItemName=`CleanText(cell[NameCol])`,
   Category=`Banner ? currentCategory : null`, Packing=`ParsePacking(cell[PackingCol], FixExcelDates)`,
   SIN, CasePrice, Brand=`BrandCol` value if set, SourceFile=uploaded file name.
6. **Skip zero/negative price** rows (can't price); count them for the "skipped" notice.

### Helper rules (ExcelDataReader returns cells as `object`)

- `NormalizeSin(o)`: if `double` and integral → `((long)d).ToString()`; else `o?.ToString()?.Trim()`.
  (Critical: FOA numeric SINs come back as `double` like `1172.0` — must become `"1172"`.)
- `CleanText(o)`: collapse whitespace, strip a leading `*`, trim; empty → null.
- `ParsePrice(o)`: if numeric → decimal; if string → strip `[^0-9.\-]` then parse; else null.
- `ParsePacking(o, fixDates)`: if `fixDates` and `o is DateTime dt` → `$"{dt.Month}/{dt.Year}"`
  (repairs values like `8/2000` that Excel stored as a date); else trimmed string.

## 7. Server-side Load sequence (the click handler / `PriceSheetImportService`)

1. Parse the uploaded file with the selected supplier's layout → `List<StagingRow>`.
2. **Validate:** ≥1 row parsed; all `CasePrice > 0` after the skip filter; `SupplierID` exists;
   `ProductYear` is 4 digits. On failure → show error, stop (nothing written).
3. `TRUNCATE dbo.SupplierPriceSheets_Staging;` then `SqlBulkCopy` the parsed rows into it.
4. `EXEC dbo.usp_ImportSupplierPriceSheets;` — it replaces that supplier+year, recomputes
   `NewItem`, and returns the summary result set.
5. Render the summary (rows / new / returning) and the detail grid; note any skipped rows.

Keep raw-ADO bits (`SqlBulkCopy`, `EXEC`) inside a `PriceSheetImportService` (registered in
`Program.cs`), mirroring how `TruckDataService` encapsulates data work.

## 8. Known edge cases (already handled by the rules above)

- **`.xls` (Hales)** — ExcelDataReader + code-page provider (section 3).
- **Multi-sheet workbooks** (SO76 "Terms"/"New for…"; FOA Sheet2/3) — only the configured sheet
  is read.
- **Preamble / split header rows** — handled by `DataStartRow`.
- **Section-header categories** (Black Market, FOA, SO76, Winco) — `Banner` logic.
- **Hales repeated SINs across brand blocks** — allowed; `SupplierPriceSheets` has no unique
  key on `(SupplierID, SIN)`. Do **not** add one.
- **Excel date-coerced packing** (Red Rhino) — `FixExcelDates`. (Winco has a few too; left as-is
  per current decision — packing accuracy isn't required for pricing.)
- **Zero-price rows** (14 in SO76) — skipped and reported, not loaded.

## 9. Config storage

Hardcode the six `SupplierLayout` records in `PriceSheetImportService` for now. **Caveat:**
sheet names and layouts drift year to year (note Winco's sheet is literally `2026 SPRING X`),
so plan to make these editable later (a `SupplierLayout` table) as part of new-supplier
onboarding. The row-count check in section 10 is the tripwire when a layout shifts.

## 10. Acceptance criteria

Loading each 2026 file through the page yields these **priced** row counts (zero-price excluded):

| Supplier | Priced rows |
|---|---|
| Black Market | 291 |
| FOA | 781 |
| Hales | 1,151 |
| Red Rhino | 615 |
| SO76 | 1,247 (1,261 parsed − 14 zero-price) |
| Winco | 591 |
| **Total** | **4,676** |

- Re-loading the same supplier/year replaces its rows (no duplicates) — verifies idempotency.
- After a load, `SupplierPriceSheets.NewItem` is populated; the summary's new/returning split
  matches a manual count vs. the prior year (or all-baseline `NULL` on the first year loaded).
- A `.xls` (Hales) and an `.xlsx` both load successfully.

## 11. Reference

The validated Python extractor (`extract_pricesheets.py`) and the SQL objects live in the
Firecracker Joe Pricesheets workspace. Treat the extractor as the executable spec for section 6
— if a parsing question arises, its behavior is the source of truth.
