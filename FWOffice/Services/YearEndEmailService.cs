using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FWOffice.Services
{
    // Generates a personalized year-end summary email for a customer by running
    // dbo.CustomerSummaryJSON and sending the prompt + JSON to a local Open WebUI
    // (OpenAI-compatible) endpoint. Config (appsettings / env vars):
    //   OpenWebUI:BaseUrl  e.g. http://thunderbird.local:8080
    //   OpenWebUI:Model    the exact model id (from GET /api/models)
    //   OpenWebUI:ApiKey   the Open WebUI API key — set via env var OpenWebUI__ApiKey (never commit it)
    public class YearEndEmailService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly string _cs;

        public YearEndEmailService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
            _cs = cfg.GetConnectionString("DefaultConnection")!;
        }

        public async Task<string> GenerateAsync(int customerId, string productYear)
        {
            var baseUrl = (_cfg["OpenWebUI:BaseUrl"] ?? "").TrimEnd('/');
            var apiKey = _cfg["OpenWebUI:ApiKey"] ?? "";
            var model = _cfg["OpenWebUI:Model"] ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("OpenWebUI:BaseUrl is not configured.");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Open WebUI API key is not configured. Set the OpenWebUI__ApiKey environment variable on the server.");
            if (string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("OpenWebUI:Model is not configured (the exact model id from GET /api/models).");

            var json = await GetSummaryJsonAsync(customerId, productYear);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"CustomerSummaryJSON returned no data for customer {customerId} in {productYear}.");

            // Single user message — gemma has no dedicated system role, so keep everything together.
            var prompt = PromptTemplate.Replace("<JSON from dbo.CustomerSummaryJSON here>", json);
            var payload = new
            {
                model,
                stream = false,
                messages = new[] { new { role = "user", content = prompt } }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(payload);

            using var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Open WebUI returned HTTP {(int)resp.StatusCode}. {Truncate(body, 500)}");

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Open WebUI returned an empty response.");
            return content.Trim();
        }

        private async Task<string> GetSummaryJsonAsync(int customerId, string productYear)
        {
            await using var con = new SqlConnection(_cs);
            await using var cmd = new SqlCommand("dbo.CustomerSummaryJSON", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@CustomerID", SqlDbType.Int).Value = customerId;
            cmd.Parameters.Add("@ProductYear", SqlDbType.Char, 4).Value = productYear;
            await con.OpenAsync();
            // SQL Server's FOR JSON output can be split across multiple rows — concatenate them.
            var sb = new StringBuilder();
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                if (!rdr.IsDBNull(0)) sb.Append(rdr.GetString(0));
            return sb.ToString();
        }

        private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

        // The email-generation prompt. The DATA placeholder is replaced with the CustomerSummaryJSON output.
        private const string PromptTemplate = """
ROLE: You are an expert business analyst and marketing consultant for "Firecracker Joe
Wholesale Fireworks," a fireworks wholesaler based in Shawnee, Oklahoma. You are friendly,
insightful, and genuinely candid — your goal is to help your retail partners grow. All dollar
figures in the data are WHOLESALE PURCHASES the retailer made from Firecracker Joe, not the
retailer's own retail sales or profit. Never describe them as the retailer's sales or profit.
TASK: Write a personalized, end-of-season business review email for a key retail partner using
the JSON data below. Use the "year" field as the current year. Produce four sections —
introduction, top-seller analysis, product-mix analysis, and three data-driven recommendations —
followed by a brief carryover caveat and a positive close.
GROUND RULES:
- Use only what's in the data. Do not invent item numbers, product names, sales figures, or
  category data. If you reference a specific product, it must appear in the data.
- Quote every number and product name EXACTLY as it appears in the data. Do not recompute,
  re-round, or change spelling. If a value isn't in the data, don't state it.
- When recommending products to add (Section 4), use ONLY items from the recommendableProducts
  list, and only from the category relevant to that recommendation. Quote the item name exactly.
  If recommendableProducts has no item in the relevant category, recommend a product TYPE instead
  and say so — never invent an item.
- Format all dollars like $76,565. Round all percentages to whole numbers.
- Keep it warm but concise and scannable: clear section headings, one short table, no walls of
  text. Aim for something a busy retailer reads in two minutes.
SECTION 1 — Introduction & High-Level Summary
- Greet the retailer warmly by first name (firstName); reference their business name
  (businessName) and city.
- State their total purchases for the year (totalPurchasesCurrentYear) and number of orders.
- Report the year-over-year change using yoyChangePercent (already calculated; it is signed):
    * If yoyChangePercent is POSITIVE: celebrate the growth and thank them for it.
    * If it is NEGATIVE: acknowledge the dip honestly and without alarm (e.g., "purchases were
      down about X% from last year"), then frame the rest of the review as the concrete plan to
      win that ground back. Do NOT call a decline "growth" or spin it as positive.
    * If it is null or zero: note this looks like their first season or a flat year and focus
      forward.
- Optionally mention their order cadence using averageOrderSize and the firstOrderDate-lastOrderDate
  span (e.g., "across N orders from June through July, averaging $X each").
SECTION 2 — Top 5 Bestsellers
- Present topSellers as a table: Rank | Item | Category | Units (unitsPurchased) | Purchased ($)
  (amountPurchased).
- Then a "Key Takeaway" paragraph identifying a real trend in THIS list — e.g., several big
  500-gram cakes signal customers who want high-impact finale items; a novelty/snap item signals
  family-and-kids traffic. Tie the takeaway to the actual items shown.
SECTION 3 — Product Mix Analysis
- Explain you're comparing their mix to the anonymous regional average AND to their own prior
  year. All percentages are share of dollars spent.
- These percentages are each category's SHARE OF TOTAL DOLLARS SPENT, not absolute sales. A low
  share does NOT mean the retailer sells little in that category — only that it's a smaller slice
  of their mix than peers'. Never describe a category as one they "don't sell," are "weak in," or
  "underperform" based on share alone.
- Treat a category as a STANDOUT STRENGTH if retailerPercentage exceeds regionalPercentage by at
  least 3 points OR is at least 1.5x the regional figure. Call out their single most distinctive
  strength (largest such gap by ratio) and mention the largest gap by absolute points if
  different.
- Treat a category as a GROWTH OPPORTUNITY if retailerPercentage is below regionalPercentage by
  at least 3 points. Name their biggest one.
- Cross-check Section 2 against Section 3 before labeling any category a growth opportunity. If a
  flagged opportunity category ALSO contains one or more of the retailer's top sellers, you MUST
  acknowledge that existing success and frame it as EXPANDING a proven category — name the existing
  seller. Example: "You already have a proven winner in Family Packs with THE GODFATHER and GOLD
  BACKYARD 4; the opportunity is that the category is a smaller share of your mix (7%) than the
  regional average (16%), so there's room to broaden the lineup with mid-range packs." Never imply
  they are absent or failing in a category where they have a bestseller.
- Where retailerPreviousPercentage is available, note any category that moved meaningfully versus
  last year (up or down by ~3+ points) — this is the most actionable signal.
- Write a short "Analysis" paragraph on what this mix suggests about their customer base and
  market position.
SECTION 4 — Three Data-Driven Recommendations
Give EXACTLY three specific, actionable recommendations, each explicitly tied to data above:
1. Double down on their standout strength from Section 3: advise expanding that category, and name
   1-2 specific items from recommendableProducts in that same category as additions worth stocking.
2. Capture their biggest growth opportunity from Section 3. If that category already contains one
   of their top sellers, position the new items as complements that broaden a proven lineup and
   name the existing seller. Otherwise, position them as a test of a new segment. Name 1-2 specific
   items from recommendableProducts in that category.
3. A smart optimization or upsell — e.g., a "good / better / best" ladder within a category that
   already sells well for them, or a complementary product to a top seller from Section 2.
CARRYOVER CAVEAT (place after Section 4, brief):
One or two sentences noting that product carried over from the prior season can affect what these
numbers show, so this review should be read alongside their own on-hand inventory.
CLOSE: A short, warm closing that uses "we"/"our" to reinforce the partnership and looks ahead to
next season.
DATA:
<JSON from dbo.CustomerSummaryJSON here>
""";
    }
}
