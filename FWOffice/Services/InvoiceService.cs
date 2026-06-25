// Services/InvoiceService.cs
// Wraps the existing invoice/billing stored procs so the Blazor page reuses the
// same logic as the legacy frmNewOrder: CustomerPrices (pricing), UpdateOrderRecord
// (header upsert), UpdateOrderDetails (line upsert), DeleteOrder, and the
// Create/UpdateOrdersTakenFromOrder warehouse-order linkage.

using System.Data;
using System.Text;
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
                    rd.IsDBNull(iPrice) ? 0m : Math.Round(rd.GetDecimal(iPrice), 2),
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

        // UpdatePaymentRecord upserts a payment and SELECTs SCOPE_IDENTITY() (new id on insert,
        // NULL on update). @PaymentID = 0 forces an insert.
        public async Task<int> SavePaymentAsync(int? paymentId, int customerId, decimal payAmount,
            DateOnly payDate, string payType, string? checkNum, string payYear)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("UpdatePaymentRecord", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PaymentID", paymentId ?? 0);
            cmd.Parameters.AddWithValue("@CustomerID", customerId);
            cmd.Parameters.AddWithValue("@PayAmount", payAmount);
            cmd.Parameters.Add("@PayDate", SqlDbType.Date).Value = payDate.ToDateTime(TimeOnly.MinValue);
            cmd.Parameters.AddWithValue("@PayType", payType);
            cmd.Parameters.AddWithValue("@CheckNum", (object?)checkNum ?? "");
            cmd.Parameters.AddWithValue("@PayHold", false);
            cmd.Parameters.AddWithValue("@JoesStand", false);
            cmd.Parameters.AddWithValue("@PayYear", payYear);
            await con.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : (paymentId ?? 0);
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

        // ---- Invoice document (faithful port of modGlobals.PrintInvoice/GenerateInvoice) ----

        // Pricesheets whose weight is computed as full cases (CaseQty = 'Y').
        private static readonly HashSet<string> CaseWeightSheets =
            new() { "UnitPrice3", "UnitPrice6", "UnitPrice7", "UnitPrice8", "UnitPrice9", "UnitPrice11" };

        private const string DocLineQuery =
            "SELECT ItemNumber, Quantity, UnitPrice, OrderID, Discount, OrderTotal, ProductName, NetItem, PackName, " +
            "DOTEXNum, FullName, BillingAddress, City, StateOrProvince, ZIPCode, Email, NetPrice, NonNetPrice, OrderDate, " +
            "StandCharges, LicenseCharges, InsuranceCharges, MiscCharges, MiscCredit, CustomerID, PhoneNumber, CellPhoneNumber, " +
            "PacksPerCase, Quantity * UnitPrice AS ExtendedPrice, UnitPrice1, UnitPrice2, UnitPrice3, UnitPrice4, UnitPrice5, " +
            "UnitPrice6, UnitPrice7, UnitPrice8, UnitPrice9, UnitPrice10, UnitPrice11, UnitPrice12, UnitPrice13, UnitPrice14, " +
            "UnitPrice15, UnitPrice16, ISNULL(CaseWeightKilos, 0.0) AS CaseWeightKilos FROM vInvoice WHERE OrderID = @OrderID ORDER BY ItemNumber";

        private const string DocItem =
            "<tr><td>{0}</td><td>{1} {2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td></tr>";

        private const string DocHead = @"<!DOCTYPE html>
<html>
<head>
<title>Firecracker Joe</title>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap.min.css'>
<link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap-theme.min.css'>
</head>
<body>
<a href='#' onclick='window.print(); return false;'>Print Invoice</a>
<div class='container'>
<table border='0' class='table'>
<tr><th><img style='float: left;' src='https://firecrackerjoe.com/img/fcman.png' width='120' height='120' alt='Firecracker Joe' /></th><th>CompNm<br>CompName2<br>Address<br>strCity, strState strZIP<br>strPhone --- strFPhone<br>FAX: strFax  --- License: strLN</th><th></th></tr>
<tr> <th>strWebsite<br>SOLD To:<br>{0}<br>{1}<br>{2}, {3} {4}<br>Order Number: {5}</th><th>Phone: {6}<br>Email: {11}<br><br><br>{7}</th><th>Cell: {8}<br><br><br>Price Sheet: {9}/{12} Cases<br>Customer Number: {10}</th></tr>
</table>
<table border ='0' class='table table-hover' style='font-size: 10px;'>
<tr><th>Item<br>Number</th><th>Qty Pack</th><th>Description</th><th>Sugg<br>Retail</th><th>Unit<br>Price</th><th>Extended<br>Price</th><th>DOTEXNum</th></tr>";

        private const string DocFoot = @"</table>
<table border='0' class='table'>
<tr><th></th><th></th><th style='text-align: right;'>Total Non-Net: {0}<span style='style'><br>Less Discount({11}): {1}</span><br>Sub-Total: {2}<br>Net Items: {3}<br>Stand Charges: {4}<br>License: {5}<br>Insurance: {6}<br>Misc Charges: {7}<br>Less Credit: {8}<br>Sales Tax: {13}<br>Total This Order: {9}<br><br>Balance To Date: {10}</th><th></th><th></th><th></th></tr>
<tr><th></th><th></th><th style='text-align: right;'>Total Weight: {12} LBS</th><th></th><th></th><th></th></tr>
</table>
<table border='1' class='table'style='font-size: 8px;'>
<tr><th colspan='6'>
*Note: The weight listed is only an estimate calculated from full case weights.
<br>This merchandise is sold upon the condition that the seller shall not be liable in any
civil action for injury occasioned during the transportation, handling, storage, sales, or use
of the merchandise. FOR RESALE</th></tr>
<tr><th colspan='6'>Please note: The buyer agrees to pay the seller a carrying charge of 1.5% per month
on any amount carried after the due date, this is equivalent to 18% per annum. This is an irrecovable
condition of sale-no exceptions. By accepting goods from seller, buyer agrees to this condition of sale.
Buyer further agrees that should seller be forced to institute collection proceedings on any unpaid
balance due to seller, buyer shall be responsible for all costs incurred by seller including
reasonable attorney fees.</th></tr>
<tr><th colspan='6'>Prices and specificatons subject to change without notice. All orders are subject to
our approval and acceptance. We will substitute when necessary. Post yourself on local, state and
federal laws; we cannot assume responsibility. No verbal agreements recognized. This order is not
subject to cancellation. ALL FIREWORKS ARE D.O.T. Consumer fireworks, UN0336, 1.4G or Articles
Pyrotechnic, UN0431, 1.4G, Professional Use Only.</th></tr>
<tr><th>Buyer hereby certifies and agrees that all goods purchased now or hereafter from the seller
are not to be delivered, possessed, stored, transhipped, distributed, sold, used or otherwise
dealt with in a manner or for a use prohibited by federal laws or by the laws of the state of
destination.</th></tr>
<tr><th><br/><br/>(RECEIVED) Conditions and terms read and accepted by: BUYER SIGNATURE:  _________________________________________________________________________</th></tr>
</table>
</div>
</body>
</html>";

        // Rebuilds the invoice HTML from current data and saves it to Orders.Invoice.
        public async Task<string> GenerateAndSaveInvoiceHtmlAsync(int orderId)
        {
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            // Order context
            string priceSheetNumber = "", orderYear = "";
            int customerId = 0;
            decimal salesTax = 0m;
            await using (var c = new SqlCommand("SELECT ISNULL(PriceSheetNumber,''), ISNULL(OrderYear,''), CustomerID, ISNULL(SalesTax,0) FROM Orders WHERE OrderID=@o", con))
            {
                c.Parameters.AddWithValue("@o", orderId);
                await using var r = await c.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    priceSheetNumber = r.GetString(0);
                    orderYear = r.GetString(1).Trim();
                    customerId = r.GetInt32(2);
                    salesTax = r.GetDecimal(3);
                }
            }

            var pricesheetName = await ScalarStrAsync(con, "SELECT PriceSheetName FROM PriceSheets WHERE PriceSheetNumber=@p", ("@p", priceSheetNumber));
            var caseQty = (await ScalarStrAsync(con, "SELECT ISNULL(CaseQty,'') FROM PriceSheets WHERE PriceSheetNumber=@p", ("@p", priceSheetNumber))).Trim();
            var retailCol = priceSheetNumber == "UnitPrice5" ? "UnitPrice14" : priceSheetNumber == "UnitPrice4" ? "UnitPrice15" : "UnitPrice13";

            // Company info
            string compNm = "", compNm2 = "", addr = "", city = "", state = "", zip = "", phone = "", fax = "", website = "", ln = "";
            await using (var c = new SqlCommand("SELECT * FROM [Our Company Info]", con))
            await using (var r = await c.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    compNm = Str(r, "CompanyName"); compNm2 = Str(r, "CompanyNameLine2"); addr = Str(r, "Address");
                    city = Str(r, "City"); state = Str(r, "StateOrProvince"); zip = Str(r, "ZIPCode");
                    phone = Str(r, "PhoneNumber"); fax = Str(r, "FaxNumber"); website = Str(r, "WebSite");
                    ln = Str(r, "WholesaleLicenseNumber");
                }
            }

            // Customer running balance for the year
            decimal custOrderTotal = 0m, payTotal = 0m;
            await using (var c = new SqlCommand("SELECT AmountType, AmountTotal FROM vOrderPayTotal WHERE CustomerID=@c AND Year=@y", con))
            {
                c.Parameters.AddWithValue("@c", customerId);
                c.Parameters.AddWithValue("@y", orderYear);
                await using var r = await c.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    if (r["AmountTotal"] == DBNull.Value) continue;
                    if (Str(r, "AmountType") == "OrderTotal") custOrderTotal = Convert.ToDecimal(r["AmountTotal"]);
                    else if (Str(r, "AmountType") == "PayTotal") payTotal = Convert.ToDecimal(r["AmountTotal"]);
                }
            }
            var balance = custOrderTotal - payTotal;

            // Net / non-net
            decimal netTotal = 0m, nonNetTotal = 0m;
            await using (var c = new SqlCommand("SELECT NetItem, SUM(NetPrice) TotalNet, SUM(NonNetPrice) TotalNonNet FROM vInvoice WHERE OrderID=@o GROUP BY NetItem", con))
            {
                c.Parameters.AddWithValue("@o", orderId);
                await using var r = await c.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    if (r["NetItem"] == DBNull.Value) continue;
                    if (Str(r, "NetItem") == "Y") netTotal = r["TotalNet"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalNet"]);
                    else nonNetTotal = r["TotalNonNet"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalNonNet"]);
                }
            }

            // Cases
            var numCases = "0";
            if (caseQty == "Y")
            {
                var s = await ScalarStrAsync(con, "SELECT SUM(Quantity) FROM [Order Details] WHERE OrderID=@o", ("@o", orderId));
                if (decimal.TryParse(s, out var nc)) numCases = nc.ToString("0.##");
            }

            // Lines
            var items = new StringBuilder();
            decimal weightPounds = 0m, decDiscount = 0m, subAfterDiscount = 0m, orderTotalRow = 0m;
            decimal standCharges = 0m, license = 0m, insurance = 0m, misc = 0m, credit = 0m;
            int intDiscount = 0;
            string head = "";
            bool headDone = false;

            await using (var c = new SqlCommand(DocLineQuery, con))
            {
                c.Parameters.AddWithValue("@OrderID", orderId);
                await using var r = await c.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var qty = Dec(r, "Quantity");
                    var unitPrice = Dec(r, "UnitPrice");
                    var extended = Dec(r, "ExtendedPrice");
                    var packsPerCase = Dec(r, "PacksPerCase");
                    var kilos = Dec(r, "CaseWeightKilos");
                    var suggRetail = r[retailCol] == DBNull.Value ? 0m : Convert.ToDecimal(r[retailCol]);

                    weightPounds += CaseWeightSheets.Contains(priceSheetNumber)
                        ? kilos * qty * 2.2046m
                        : (packsPerCase != 0 ? (kilos / packsPerCase) * qty * 2.2046m : 0m);

                    var packName = Str(r, "PackName");
                    var desc = Str(r, "ProductName") + " - " + ((int)packsPerCase) + packName + "/Case";
                    var orderType = caseQty == "Y" ? "CS" : packName;
                    items.Append(string.Format(DocItem,
                        Str(r, "ItemNumber"), qty.ToString("0.####"), orderType, desc,
                        suggRetail.ToString("c"), unitPrice.ToString("c"), extended.ToString("c"), Str(r, "DOTEXNum")));

                    var discount = Dec(r, "Discount");
                    intDiscount = (int)(discount * 100);
                    decDiscount = nonNetTotal * discount;
                    subAfterDiscount = nonNetTotal - decDiscount;
                    orderTotalRow = Dec(r, "OrderTotal");
                    standCharges = Dec(r, "StandCharges");
                    license = Dec(r, "LicenseCharges");
                    insurance = Dec(r, "InsuranceCharges");
                    misc = Dec(r, "MiscCharges");
                    credit = Dec(r, "MiscCredit");

                    if (!headDone)
                    {
                        head = string.Format(FillCompany(DocHead, compNm, compNm2, addr, city, state, zip, phone, fax, website, ln),
                            Str(r, "FullName"), Str(r, "BillingAddress"), Str(r, "City"), Str(r, "StateOrProvince"),
                            Str(r, "ZIPCode"), Str(r, "OrderID"), Str(r, "PhoneNumber"), Str(r, "OrderDate"),
                            Str(r, "CellPhoneNumber"), pricesheetName, Str(r, "CustomerID"), Str(r, "Email"), numCases);
                        headDone = true;
                    }
                }
            }

            if (!headDone)
            {
                head = string.Format(FillCompany(DocHead, compNm, compNm2, addr, city, state, zip, phone, fax, website, ln),
                    "", "", "", "", "", orderId.ToString(), "", "", "", pricesheetName, customerId.ToString(), "", numCases);
            }

            var foot = string.Format(DocFoot,
                nonNetTotal.ToString("c"), decDiscount.ToString("c"), subAfterDiscount.ToString("c"), netTotal.ToString("c"),
                standCharges.ToString("c"), license.ToString("c"), insurance.ToString("c"), misc.ToString("c"),
                credit.ToString("c"), orderTotalRow.ToString("c"), balance.ToString("c"), " " + intDiscount + "%",
                weightPounds.ToString("0.0"), salesTax.ToString("0.00"));
            foot = intDiscount > 0
                ? foot.Replace("style='style'", "style='display: default;'")
                : foot.Replace("style='style'", "style='display: none;'");

            var shorts = await BuildShortsAsync(con, orderId);

            var html = head + items + shorts + foot;

            await using (var c = new SqlCommand("UPDATE Orders SET Invoice = @i WHERE OrderID = @o", con))
            {
                c.Parameters.AddWithValue("@i", html);
                c.Parameters.AddWithValue("@o", orderId);
                await c.ExecuteNonQueryAsync();
            }
            return html;
        }

        // ---- Customer statement (faithful port of modGlobals.PrintStatement, from vStatement) ----

        private const string StmtLineSql =
            "select SettleType, OrderID, OrderDate, PriceSheetName, " +
            "FORMAT(Discount,'P') as 'Discount', FORMAT(StandCharges,'c') as 'StandCharges', " +
            "FORMAT(LicenseCharges,'c') as 'LicenseCharges', FORMAT(InsuranceCharges,'c') as 'InsuranceCharges', " +
            "FORMAT(NetTotal,'c') as 'NetTotal', " +
            "FORMAT(OrderT-NetTotal-StandCharges-LicenseCharges-InsuranceCharges-MiscCharges,'c') as 'NonNet', " +
            "FORMAT(OrderTotal,'c') as 'OrderTotal', FullName, BillingAddress, City, StateOrProvince, ZIPCode, CustomerID, " +
            "FORMAT(NetTotal+(OrderT-NetTotal-StandCharges-LicenseCharges-InsuranceCharges-MiscCharges),'c') as 'NetNon' " +
            "from vStatement WHERE CustomerID = @CustomerID And Year = @Year ORDER BY OrderID";

        private const string StmtSummarySql =
            "select FORMAT(SUM(StandCharges),'c') as 'StandCharges', FORMAT(SUM(LicenseCharges),'c') as 'LicenseCharges', " +
            "FORMAT(SUM(InsuranceCharges),'c') as 'InsuranceCharges', FORMAT(SUM(NetTotal),'c') as 'NetTotal', " +
            "FORMAT(SUM(OrderT-NetTotal-StandCharges-LicenseCharges-InsuranceCharges-MiscCharges+MiscCredit),'c') as 'NonNet', " +
            "FORMAT(SUM(OrderT-StandCharges-LicenseCharges-InsuranceCharges-MiscCharges+MiscCredit),'c') as 'NetNon', " +
            "FORMAT(SUM(StandCharges+LicenseCharges+InsuranceCharges),'c') as 'SLI', FORMAT(SUM(MiscCharges),'c') as 'MiscCharges', " +
            "FORMAT(SUM(MiscCredit),'c') as 'MiscCredit', FORMAT(SUM(OrderT),'c') as 'OrderT', FORMAT(SUM(PaymentT),'c') as 'PaymentT', " +
            "FORMAT(SUM(OrderT)-SUM(PaymentT),'c') as 'BalDue' from vStatement WHERE CustomerID = @CustomerID And Year = @Year";

        private const string StmtItem =
            "<tr><td>{0}</td><td>{1} {2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td><td>{9}</td><td>{10}</td></tr>";

        private const string StmtHead = @"<!DOCTYPE html>
<html>
<head>
<title>Firecracker Joe</title>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap.min.css'>
<link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap-theme.min.css'>
</head>
<body>
<a href='#' onclick='window.print(); return false;'>Print Statement</a>
<div class='container'>
<table border='0' class='table'>
<tr><th><img style='float: left;' src='https://firecrackerjoe.com/img/fcman.png' width='120' height='120' alt='Firecracker Joe' /></th><th>CompNm<br>CompName2<br>Address<br>strCity, strState strZIP<br>strPhone --- strFPhone<br>FAX: strFax  --- License: strLN</th><th></th></tr>
<tr> <th>strWebsite<br>Customer:<br>{0}<br>{1}<br>{2}, {3} {4}<br>Number: {5}</th><th>Final Statement<br>(Orders discounted on invoices)<br><br><br></th><th><br><br><br><br>{6}</th></tr>
</table>
<table border ='0' class='table table-hover' style='font-size: 10px;'>
<tr><th>Type</th><th>Num Date</th><th>Price Sheet</th><th>Discount</th><th>Stand</th><th>Lic.</th><th>Insur.</th><th>Net</th><th>Non-Net</th><th>Total</th></tr>";

        private const string StmtFoot = @"</table>
<table border='0' class='table'>
<tr><th></th><th></th><th style='text-align: right;'>Total Non-Net Less ({0}): {1}<br>Plus Total Net Items: {2}<br><br>= {3}<br><br><br>Plus SLI Charges: {4}<br>Plus Misc Charges: {5}<br>Less Misc Credits: {6}<br><br>= {7}<br>Less Payments: {8}<br><br>= Balance Due: {9}</th><th></th><th></th><th></th></tr>
</table>
</div>
</body>
</html>";

        public async Task<string> GenerateStatementHtmlAsync(int customerId, int year)
        {
            var yearStr = year.ToString();
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            var co = await LoadCompanyAsync(con);

            var items = new StringBuilder();
            string head = "";
            bool headDone = false;
            string lastDiscount = "";

            await using (var cmd = new SqlCommand(StmtLineSql, con))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.Parameters.AddWithValue("@Year", yearStr);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    lastDiscount = Str(rd, "Discount");
                    items.Append(string.Format(StmtItem,
                        Str(rd, "SettleType"), Str(rd, "OrderID"), FmtDate(rd["OrderDate"]), Str(rd, "PriceSheetName"),
                        Str(rd, "Discount"), Str(rd, "StandCharges"), Str(rd, "LicenseCharges"), Str(rd, "InsuranceCharges"),
                        Str(rd, "NetTotal"), Str(rd, "NonNet"), Str(rd, "OrderTotal")));
                    if (!headDone)
                    {
                        head = string.Format(FillCompany(StmtHead, co.compNm, co.compNm2, co.addr, co.city, co.state, co.zip, co.phone, co.fax, co.website, co.ln),
                            Str(rd, "FullName"), Str(rd, "BillingAddress"), Str(rd, "City"), Str(rd, "StateOrProvince"), Str(rd, "ZIPCode"),
                            customerId.ToString(), " ");
                        headDone = true;
                    }
                }
            }

            if (!headDone)
            {
                string fn = "", ba = "", ct = "", st = "", zp = "";
                await using (var cmd = new SqlCommand("SELECT FullName, BillingAddress, City, StateOrProvince, ZIPCode FROM Customers WHERE CustomerID=@c", con))
                {
                    cmd.Parameters.AddWithValue("@c", customerId);
                    await using var rd = await cmd.ExecuteReaderAsync();
                    if (await rd.ReadAsync()) { fn = Str(rd, "FullName"); ba = Str(rd, "BillingAddress"); ct = Str(rd, "City"); st = Str(rd, "StateOrProvince"); zp = Str(rd, "ZIPCode"); }
                }
                head = string.Format(FillCompany(StmtHead, co.compNm, co.compNm2, co.addr, co.city, co.state, co.zip, co.phone, co.fax, co.website, co.ln),
                    fn, ba, ct, st, zp, customerId.ToString(), " ");
            }

            string foot = "";
            await using (var cmd = new SqlCommand(StmtSummarySql, con))
            {
                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                cmd.Parameters.AddWithValue("@Year", yearStr);
                await using var rd = await cmd.ExecuteReaderAsync();
                if (await rd.ReadAsync())
                {
                    foot = string.Format(StmtFoot,
                        lastDiscount, Str(rd, "NonNet"), Str(rd, "NetTotal"), Str(rd, "NetNon"), Str(rd, "SLI"),
                        Str(rd, "MiscCharges"), Str(rd, "MiscCredit"), Str(rd, "OrderT"), Str(rd, "PaymentT"), Str(rd, "BalDue"));
                }
            }

            return head + items + foot;
        }

        private async Task<(string compNm, string compNm2, string addr, string city, string state, string zip, string phone, string fax, string website, string ln)> LoadCompanyAsync(SqlConnection con)
        {
            await using var c = new SqlCommand("SELECT * FROM [Our Company Info]", con);
            await using var r = await c.ExecuteReaderAsync();
            if (await r.ReadAsync())
                return (Str(r, "CompanyName"), Str(r, "CompanyNameLine2"), Str(r, "Address"), Str(r, "City"), Str(r, "StateOrProvince"),
                    Str(r, "ZIPCode"), Str(r, "PhoneNumber"), Str(r, "FaxNumber"), Str(r, "WebSite"), Str(r, "WholesaleLicenseNumber"));
            return ("", "", "", "", "", "", "", "", "", "");
        }

        private static string FmtDate(object? o) => o is DateTime d ? d.ToString("M/d/yyyy") : (o?.ToString() ?? "");

        private static string FillCompany(string template, string compNm, string compNm2, string addr, string city,
            string state, string zip, string phone, string fax, string website, string ln)
            => template
                .Replace("strLN", ln).Replace("CompName2", compNm2).Replace("CompNm", compNm)
                .Replace("Address", addr).Replace("strCity", city).Replace("strState", state)
                .Replace("strZIP", zip).Replace("strPhone", phone).Replace("strFPhone", fax)
                .Replace("strFax", fax).Replace("strWebsite", website);

        private async Task<string> BuildShortsAsync(SqlConnection con, int orderId)
        {
            int? otid = null;
            await using (var c = new SqlCommand("SELECT OrdersTakenID FROM Orders WHERE OrderID=@o", con))
            {
                c.Parameters.AddWithValue("@o", orderId);
                var o = await c.ExecuteScalarAsync();
                if (o != null && o != DBNull.Value) otid = Convert.ToInt32(o);
            }
            if (otid == null) return "";

            var sb = new StringBuilder();
            sb.Append("</table>--Items Shorted--<table class='table table-hover' style='font-size: 10px;'>");
            sb.Append("<th>Item<br>Number</th><th>Description</th><th>Quantity<br>Ordered</th><th>Quantity<br>Pulled</th>");
            await using (var c = new SqlCommand(
                "SELECT ItemNumber, ProductName, QuantityOrdered, QuantityPulled FROM OrdersTakenDetails " +
                "INNER JOIN Products ON OrdersTakenDetails.ProductID = Products.ProductID " +
                "WHERE OrdersTakenId=@otid AND NOT QuantityOrdered = QuantityPulled", con))
            {
                c.Parameters.AddWithValue("@otid", otid.Value);
                await using var r = await c.ExecuteReaderAsync();
                int n = 0;
                while (await r.ReadAsync())
                {
                    n++;
                    sb.Append(n % 2 == 0 ? "</tr><tr class='hilite'>" : "</tr><tr>");
                    sb.Append("<td>").Append(Str(r, "ItemNumber")).Append("</td>");
                    sb.Append("<td>").Append(Str(r, "ProductName")).Append("</td>");
                    sb.Append("<td>").Append(Str(r, "QuantityOrdered")).Append("</td>");
                    sb.Append("<td>").Append(Str(r, "QuantityPulled")).Append("</td>");
                }
            }
            return sb.ToString();
        }

        private static string Str(SqlDataReader r, string col) { var o = r[col]; return o == DBNull.Value ? "" : o.ToString() ?? ""; }
        private static decimal Dec(SqlDataReader r, string col) { var o = r[col]; return o == DBNull.Value ? 0m : Convert.ToDecimal(o); }

        private async Task<string> ScalarStrAsync(SqlConnection con, string sql, params (string name, object val)[] ps)
        {
            var owns = con.State != ConnectionState.Open;
            if (owns) await con.OpenAsync();
            try
            {
                await using var c = new SqlCommand(sql, con);
                foreach (var (name, val) in ps) c.Parameters.AddWithValue(name, val);
                var o = await c.ExecuteScalarAsync();
                return (o == null || o == DBNull.Value) ? "" : o.ToString() ?? "";
            }
            finally { if (owns) await con.CloseAsync(); }
        }
    }
}
