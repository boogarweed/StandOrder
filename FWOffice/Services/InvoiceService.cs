// Services/InvoiceService.cs
// Wraps the existing invoice/billing stored procs so the Blazor page reuses the
// same logic as the legacy frmNewOrder: CustomerPrices (pricing), UpdateOrderRecord
// (header upsert), UpdateOrderDetails (line upsert), DeleteOrder, and the
// Create/UpdateOrdersTakenFromOrder warehouse-order linkage.

using System.Data;
using Microsoft.Data.SqlClient;

namespace FWOffice.Services
{
    public class InvoiceService
    {
        private readonly string _cs;

        public InvoiceService(IConfiguration config) =>
            _cs = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        public record PricedProduct(int ProductId, int? ItemNumber, string? ProductName, decimal UnitPrice, bool NetItem);

        public record InvoiceHeader(
            int? OrderId, int CustomerId, string PriceSheetNumber, decimal OrderTotal,
            decimal LicenseCharges, decimal InsuranceCharges, decimal MiscCharges, decimal NetTotal,
            string OrderYear, int? OrdersTakenId, decimal SalesTax, string ChargeText, string Comment);

        // CustomerPrices: every product for the year priced from the chosen pricesheet column,
        // with any customer-specific override already applied (output price column = "UnitPrice1").
        public async Task<List<PricedProduct>> GetPricedProductsAsync(int customerId, string pricesheet, string year)
        {
            var list = new List<PricedProduct>();
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("CustomerPrices", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CustomerID", customerId);
            cmd.Parameters.AddWithValue("@pricesheet", pricesheet);
            cmd.Parameters.AddWithValue("@year", year);
            await con.OpenAsync();
            await using var rd = await cmd.ExecuteReaderAsync();
            int iPid = rd.GetOrdinal("ProductID"), iItem = rd.GetOrdinal("ItemNumber"),
                iName = rd.GetOrdinal("ProductName"), iPrice = rd.GetOrdinal("UnitPrice1"),
                iNet = rd.GetOrdinal("NetItem");
            while (await rd.ReadAsync())
            {
                list.Add(new PricedProduct(
                    rd.GetInt32(iPid),
                    rd.IsDBNull(iItem) ? null : rd.GetInt32(iItem),
                    rd.IsDBNull(iName) ? null : rd.GetString(iName),
                    rd.IsDBNull(iPrice) ? 0m : rd.GetDecimal(iPrice),
                    !rd.IsDBNull(iNet) && string.Equals(rd.GetString(iNet).Trim(), "Y", StringComparison.OrdinalIgnoreCase)));
            }
            return list;
        }

        // UpdateOrderRecord upserts the invoice header and SELECTs SCOPE_IDENTITY()
        // (the new OrderID on insert, NULL on update). @OrderID = 0 forces an insert.
        public async Task<int> SaveHeaderAsync(InvoiceHeader h)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("UpdateOrderRecord", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CustomerID", h.CustomerId);
            cmd.Parameters.AddWithValue("@PriceSheetNumber", h.PriceSheetNumber);
            cmd.Parameters.AddWithValue("@OrderTotal", h.OrderTotal);
            cmd.Parameters.AddWithValue("@StandCharges", 0m);
            cmd.Parameters.AddWithValue("@LicenseCharges", h.LicenseCharges);
            cmd.Parameters.AddWithValue("@InsuranceCharges", h.InsuranceCharges);
            cmd.Parameters.AddWithValue("@MiscCharges", h.MiscCharges);
            cmd.Parameters.AddWithValue("@NetTotal", h.NetTotal);
            cmd.Parameters.AddWithValue("@OrderYear", h.OrderYear);
            cmd.Parameters.AddWithValue("@OrdersTakenID", (object?)h.OrdersTakenId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderID", (object?)h.OrderId ?? 0);
            cmd.Parameters.AddWithValue("@SalesTax", h.SalesTax);
            cmd.Parameters.AddWithValue("@ChargeText", h.ChargeText);
            cmd.Parameters.AddWithValue("@Comment", h.Comment);
            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : (h.OrderId ?? 0);
        }

        public async Task SaveLineAsync(int orderId, int productId, decimal qty, decimal unitPrice)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("UpdateOrderDetails", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            cmd.Parameters.AddWithValue("@ProductID", productId);
            cmd.Parameters.AddWithValue("@Quantity", (double)qty);
            cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
            cmd.Parameters.AddWithValue("@OrderDetailID", 0);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteLineAsync(int orderId, int productId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("DELETE FROM [Order Details] WHERE OrderID = @o AND ProductID = @p", con);
            cmd.Parameters.AddWithValue("@o", orderId);
            cmd.Parameters.AddWithValue("@p", productId);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteInvoiceAsync(int orderId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("DeleteOrder", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int?> GetLinkedOrdersTakenIdAsync(int orderId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("SELECT OrdersTakenID FROM Orders WHERE OrderID = @o", con);
            cmd.Parameters.AddWithValue("@o", orderId);
            await con.OpenAsync();
            var r = await cmd.ExecuteScalarAsync();
            return (r == null || r == DBNull.Value) ? null : Convert.ToInt32(r);
        }

        public async Task SetLinkedOrdersTakenIdAsync(int orderId, int ordersTakenId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("UPDATE Orders SET OrdersTakenID = @ot WHERE OrderID = @o", con);
            cmd.Parameters.AddWithValue("@ot", ordersTakenId);
            cmd.Parameters.AddWithValue("@o", orderId);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task CreateOrdersTakenFromOrderAsync(int orderId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("CreateOrdersTakenFromOrder", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID_Param", orderId);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateOrdersTakenFromOrderAsync(int orderId)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("UpdateOrdersTakenFromOrder", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
