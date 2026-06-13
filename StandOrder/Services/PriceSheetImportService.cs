// Services/PriceSheetImportService.cs
// Parses a supplier's Excel price sheet (per a per-supplier layout) and loads it into
// dbo.SupplierPriceSheets via the staging table + dbo.usp_ImportSupplierPriceSheets.
// See docs/PriceSheetLoad-BuildSpec.md. The Python extract_pricesheets.py is the spec of record.

using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Microsoft.Data.SqlClient;

namespace StandOrder.Services
{
    public class PriceSheetImportService
    {
        private readonly string _connectionString;

        public PriceSheetImportService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        // ---- per-supplier layout config (columns are 0-based indexes) ----
        public record RepeatHeader(int LabelCol, string LabelValue, int[] GroupCols, string[] SkipLabels);

        public record SupplierLayout(
            int SupplierId,
            string SheetName,
            int DataStartRow,
            int SinCol,
            int NameCol,
            int PackingCol,
            int CasePriceCol,
            int? BrandCol,
            bool Banner,
            bool FixExcelDates,
            RepeatHeader? RepeatHdr);

        private static readonly Dictionary<int, SupplierLayout> Layouts = new()
        {
            [1]  = new(1,  "PriceList",      3, SinCol: 0, NameCol: 1, PackingCol: 2, CasePriceCol: 3, BrandCol: null, Banner: true,  FixExcelDates: false, RepeatHdr: null),
            [2]  = new(2,  "Sheet1",         8, SinCol: 1, NameCol: 2, PackingCol: 3, CasePriceCol: 6, BrandCol: null, Banner: true,  FixExcelDates: false, RepeatHdr: null),
            [4]  = new(4,  "Sheet1",         9, SinCol: 0, NameCol: 2, PackingCol: 3, CasePriceCol: 4, BrandCol: null, Banner: true,  FixExcelDates: false,
                       RepeatHdr: new RepeatHeader(LabelCol: 0, LabelValue: "Item #", GroupCols: new[] { 2, 1 }, SkipLabels: new[] { "QTY", "PACKING", "PRICE", "TOTAL" })),
            [5]  = new(5,  "Sheet1",         1, SinCol: 0, NameCol: 2, PackingCol: 5, CasePriceCol: 6, BrandCol: 3,    Banner: false, FixExcelDates: true,  RepeatHdr: null),
            [10] = new(10, "Price List",     1, SinCol: 0, NameCol: 1, PackingCol: 2, CasePriceCol: 6, BrandCol: 3,    Banner: true,  FixExcelDates: false, RepeatHdr: null),
            [7]  = new(7,  "2026 SPRING X",  8, SinCol: 0, NameCol: 3, PackingCol: 5, CasePriceCol: 8, BrandCol: 2,    Banner: true,  FixExcelDates: false, RepeatHdr: null),
        };

        public bool HasLayout(int supplierId) => Layouts.ContainsKey(supplierId);

        public class StagingRow
        {
            public int SupplierID { get; set; }
            public string ProductYear { get; set; } = "";
            public string? ItemName { get; set; }
            public string? Category { get; set; }
            public string? Packing { get; set; }
            public string? SIN { get; set; }
            public decimal CasePrice { get; set; }
            public string? Brand { get; set; }
            public string? SourceFile { get; set; }
        }

        public class ImportResult
        {
            public bool Success { get; set; }
            public string? Error { get; set; }
            public int ParsedRows { get; set; }
            public int SkippedNoPrice { get; set; }
            public int LoadedRows { get; set; }
            public int NewItems { get; set; }
            public int Returning { get; set; }
            public int Baseline { get; set; }
        }

        public async Task<ImportResult> ImportAsync(int supplierId, string productYear, Stream excelStream, string fileName)
        {
            if (!Layouts.TryGetValue(supplierId, out var layout))
                return new ImportResult { Success = false, Error = $"No price-sheet layout is configured for supplier {supplierId}." };

            if (!Regex.IsMatch(productYear ?? "", @"^\d{4}$"))
                return new ImportResult { Success = false, Error = "Year must be 4 digits." };

            List<StagingRow> rows;
            int skipped;
            try
            {
                rows = Parse(layout, productYear!, excelStream, fileName, out skipped);
            }
            catch (Exception ex)
            {
                return new ImportResult { Success = false, Error = $"Could not read the file: {ex.Message}" };
            }

            if (rows.Count == 0)
                return new ImportResult { Success = false, Error = "No priced rows were found. Check that the supplier and file match.", SkippedNoPrice = skipped };

            try
            {
                var (loaded, news, ret, baseline) = await LoadAsync(supplierId, productYear!, rows);
                return new ImportResult
                {
                    Success = true,
                    ParsedRows = rows.Count + skipped,
                    SkippedNoPrice = skipped,
                    LoadedRows = loaded,
                    NewItems = news,
                    Returning = ret,
                    Baseline = baseline
                };
            }
            catch (Exception ex)
            {
                return new ImportResult { Success = false, Error = $"Load failed: {ex.Message}" };
            }
        }

        // ---- parsing (port of extract_pricesheets.py) ----
        private static List<StagingRow> Parse(SupplierLayout layout, string year, Stream stream, string fileName, out int skippedNoPrice)
        {
            skippedNoPrice = 0;
            if (stream.CanSeek) stream.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });

            var table = ds.Tables[layout.SheetName]
                ?? throw new InvalidOperationException($"Sheet '{layout.SheetName}' not found in the workbook.");

            var result = new List<StagingRow>();
            string? category = null;
            int cols = table.Columns.Count;

            object? Cell(DataRow r, int c) => (c >= 0 && c < cols && r[c] != DBNull.Value) ? r[c] : null;

            for (int i = layout.DataStartRow; i < table.Rows.Count; i++)
            {
                var r = table.Rows[i];

                // 1) repeating header row (Hales brand blocks) -> update category, skip
                if (layout.RepeatHdr is RepeatHeader rh)
                {
                    var label = CleanText(Cell(r, rh.LabelCol));
                    if (label != null && string.Equals(label, rh.LabelValue, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var gc in rh.GroupCols)
                        {
                            var grp = CleanText(Cell(r, gc));
                            if (grp != null && !rh.SkipLabels.Contains(grp, StringComparer.OrdinalIgnoreCase))
                            {
                                category = grp;
                                break;
                            }
                        }
                        continue;
                    }
                }

                // 2) price
                var price = ParsePrice(Cell(r, layout.CasePriceCol));

                // 3) banner / section-header row (no price)
                if (price is null)
                {
                    if (layout.Banner)
                    {
                        var txt = CleanText(Cell(r, 0));
                        if (txt != null && !IsFooter(txt))
                            category = txt;
                    }
                    continue;
                }

                // 4) need a SIN
                var sin = NormalizeSin(Cell(r, layout.SinCol));
                if (string.IsNullOrEmpty(sin)) continue;

                // 6) skip zero / negative price
                if (price.Value <= 0m) { skippedNoPrice++; continue; }

                // 5) emit
                result.Add(new StagingRow
                {
                    SupplierID = layout.SupplierId,
                    ProductYear = year,
                    ItemName = CleanText(Cell(r, layout.NameCol)),
                    Category = layout.Banner ? category : null,
                    Packing = ParsePacking(Cell(r, layout.PackingCol), layout.FixExcelDates),
                    SIN = sin,
                    CasePrice = price.Value,
                    Brand = layout.BrandCol is int bc ? CleanText(Cell(r, bc)) : null,
                    SourceFile = fileName
                });
            }

            return result;
        }

        // ---- DB load: staging -> proc -> summary ----
        private async Task<(int loaded, int news, int ret, int baseline)> LoadAsync(int supplierId, string year, List<StagingRow> rows)
        {
            var dt = new DataTable();
            dt.Columns.Add("SupplierID", typeof(int));
            dt.Columns.Add("ProductYear", typeof(string));
            dt.Columns.Add("ItemName", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("Packing", typeof(string));
            dt.Columns.Add("SIN", typeof(string));
            dt.Columns.Add("CasePrice", typeof(decimal));
            dt.Columns.Add("Brand", typeof(string));
            dt.Columns.Add("SourceFile", typeof(string));

            foreach (var x in rows)
            {
                dt.Rows.Add(
                    x.SupplierID,
                    x.ProductYear,
                    (object?)x.ItemName ?? DBNull.Value,
                    (object?)x.Category ?? DBNull.Value,
                    (object?)x.Packing ?? DBNull.Value,
                    (object?)x.SIN ?? DBNull.Value,
                    x.CasePrice,
                    (object?)x.Brand ?? DBNull.Value,
                    (object?)x.SourceFile ?? DBNull.Value);
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using (var cmd = new SqlCommand("TRUNCATE TABLE dbo.SupplierPriceSheets_Staging", conn))
                await cmd.ExecuteNonQueryAsync();

            using (var bulk = new SqlBulkCopy(conn) { DestinationTableName = "dbo.SupplierPriceSheets_Staging" })
            {
                foreach (DataColumn c in dt.Columns)
                    bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                await bulk.WriteToServerAsync(dt);
            }

            await using (var cmd = new SqlCommand("EXEC dbo.usp_ImportSupplierPriceSheets", conn) { CommandTimeout = 120 })
                await cmd.ExecuteNonQueryAsync();

            const string summarySql = @"
SELECT COUNT(*) AS Loaded,
       SUM(CASE WHEN NewItem = 1 THEN 1 ELSE 0 END) AS NewItems,
       SUM(CASE WHEN NewItem = 0 THEN 1 ELSE 0 END) AS Returning,
       SUM(CASE WHEN NewItem IS NULL THEN 1 ELSE 0 END) AS Baseline
FROM dbo.SupplierPriceSheets
WHERE SupplierID = @s AND ProductYear = @y;";

            await using (var cmd = new SqlCommand(summarySql, conn))
            {
                cmd.Parameters.AddWithValue("@s", supplierId);
                cmd.Parameters.AddWithValue("@y", year);
                await using var rd = await cmd.ExecuteReaderAsync();
                if (await rd.ReadAsync())
                {
                    int loaded = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                    int news = rd.IsDBNull(1) ? 0 : rd.GetInt32(1);
                    int ret = rd.IsDBNull(2) ? 0 : rd.GetInt32(2);
                    int baseline = rd.IsDBNull(3) ? 0 : rd.GetInt32(3);
                    return (loaded, news, ret, baseline);
                }
            }
            return (0, 0, 0, 0);
        }

        // ---- helpers (match the Python extractor) ----
        private static string? NormalizeSin(object? o)
        {
            if (o is null) return null;
            if (o is double d && d == Math.Floor(d)) return ((long)d).ToString(CultureInfo.InvariantCulture);
            return o.ToString()?.Trim();
        }

        private static string? CleanText(object? o)
        {
            if (o is null) return null;
            var s = Regex.Replace(o.ToString() ?? "", @"\s+", " ").Trim();
            s = s.TrimStart('*').Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static decimal? ParsePrice(object? o)
        {
            if (o is null) return null;
            if (o is double d) return (decimal)d;
            var s = Regex.Replace(o.ToString() ?? "", @"[^0-9.\-]", "");
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null;
        }

        private static string? ParsePacking(object? o, bool fixDates)
        {
            if (o is null) return null;
            if (fixDates && o is DateTime dt) return $"{dt.Month}/{dt.Year}";
            var s = Regex.Replace(o.ToString() ?? "", @"\s+", " ").Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static bool IsFooter(string s) =>
            Regex.IsMatch(s, @"subject to change|prices|total", RegexOptions.IgnoreCase);
    }
}
