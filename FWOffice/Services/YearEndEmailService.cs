using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FWOffice.Services
{
    // A referenced product found in the generated email, with its real category from the data.
    // InData=false means the email named an item number that isn't in the JSON at all (invented).
    // NameMatches=false means the email paired this item number with a name that differs from the data.
    public record ProductRef(string Name, string? Category, bool InData, bool NameMatches);

    // Result of a generation: the email text plus the product-reference audit.
    public record EmailResult(string Email, List<ProductRef> Products);

    // Generates a personalized year-end summary email for a customer by running
    // dbo.CustomerSummaryJSON and sending the prompt + JSON to a local Open WebUI
    // (OpenAI-compatible) endpoint, then audits which products the email names.
    // Config: OpenWebUI:BaseUrl / Model (appsettings) and OpenWebUI:ApiKey (env var OpenWebUI__ApiKey).
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

        public async Task<EmailResult> GenerateAsync(int customerId, string productYear)
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
            content = content.Trim();

            return new EmailResult(content, BuildProductAudit(json, content));
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

        // Cross-check every "<number> -- ..." item the email names against the data. Known items
        // report their real category (so a reviewer can catch a wrong-category recommendation);
        // unknown item numbers are flagged as not-in-data (invented).
        private static List<ProductRef> BuildProductAudit(string json, string email)
        {
            var known = new Dictionary<int, (string name, string category)>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                foreach (var arrName in new[] { "topSellers", "recommendableProducts" })
                {
                    if (!root.TryGetProperty(arrName, out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
                    foreach (var e in arr.EnumerateArray())
                    {
                        var name = e.TryGetProperty("itemName", out var n) ? n.GetString() : null;
                        var cat = e.TryGetProperty("category", out var c) ? c.GetString() : null;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var m = Regex.Match(name, @"^\s*(\d+)");
                        if (m.Success && int.TryParse(m.Groups[1].Value, out var num))
                            known.TryAdd(num, (name.Trim(), cat ?? ""));
                    }
                }
            }
            catch { /* if the JSON can't be parsed, skip the audit rather than fail generation */ }

            var normEmail = Regex.Replace(email, @"\s+", " ");
            var refs = new List<ProductRef>();
            var seen = new HashSet<int>();
            foreach (Match m in Regex.Matches(email, @"(\d+)\s*--"))
            {
                if (!int.TryParse(m.Groups[1].Value, out var num) || !seen.Add(num)) continue;
                if (known.TryGetValue(num, out var k))
                {
                    // Real number — does the email quote the same product name the data has for it?
                    var normName = Regex.Replace(k.name, @"\s+", " ");
                    var nameMatches = normEmail.Contains(normName, StringComparison.OrdinalIgnoreCase);
                    refs.Add(new ProductRef(k.name, k.category, true, nameMatches));
                }
                else
                    refs.Add(new ProductRef($"item #{num} (named in the email)", null, false, false));
            }
            return refs;
        }

        private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

        // Email-generation prompt (plain text, no Markdown; guardrails on months, categories, and
        // the growth-opportunity threshold). The DATA placeholder is replaced with the proc output.
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
OUTPUT FORMAT (IMPORTANT):
- Write PLAIN TEXT ready to paste directly into an email. Do NOT use Markdown of any kind:
  no asterisks or **bold**, no # headings, no backticks, and NO pipe "|" tables.
- Use plain UPPERCASE section headers on their own line (for example a line that reads: TOP SELLERS).
- Present any list as simple text lines, one item per line.
- Put a "Subject:" line at the very top.
GROUND RULES:
- Use only what's in the data. Do not invent item numbers, product names, sales figures, or
  category data. If you reference a specific product, it must appear in the data, quoted EXACTLY —
  including its leading item number.
- Quote every number and product name EXACTLY as it appears in the data. Do not recompute,
  re-round, or change spelling. If a value isn't in the data, don't state it.
- Format all dollars like $76,565. Round all percentages to whole numbers, EXCEPT quote
  yoyChangePercent exactly as given (it may include a decimal).
- Keep it warm but concise and scannable: clear plain-text section headers, no walls of text.
  Aim for something a busy retailer reads in two minutes.
SECTION 1 — Introduction & High-Level Summary
- Greet the retailer warmly by first name (firstName). If businessName is present, reference it;
  otherwise just reference their city. Always mention the city.
- State their total purchases for the year (totalPurchasesCurrentYear) and number of orders.
- Report the year-over-year change using yoyChangePercent (already calculated; it is signed):
    * POSITIVE: celebrate the growth and thank them.
    * NEGATIVE: acknowledge the dip honestly and without alarm ("purchases were down about X% from
      last year"), then frame the rest as the concrete plan to win that ground back. Never call a
      decline "growth."
    * If numberOfOrders is 0 or there is no current-year purchasing:
        - If totalPurchasesPreviousYear is present and greater than 0, this is a VALUED RETURNING
          partner who simply hasn't ordered yet THIS season. Warmly say you haven't seen an order
          from them this season, acknowledge they were a good customer last year, and frame the
          email as a friendly invitation to come back — never as a brand-new or unknown store.
        - Otherwise (no prior year either), treat it as a likely first season and focus forward.
- Mention order cadence using averageOrderSize and the firstOrderDate–lastOrderDate span. DERIVE
  the month range from those two dates in the data (e.g., 2026-05-30 to 2026-06-18 is "late May
  through mid-June"). Do NOT reuse any example month range from this prompt — read the actual dates.
SECTION 2 — Top 5 Bestsellers
- If topSellers is empty, state plainly there are no purchases to rank this season and skip the
  list and takeaway.
- List topSellers as one line each (NO table), for example:
    1. 2640 -- PARTY PACK 4  (FAMILY PACKS) — 12 units — $3,056
  Quote itemName, category, unitsPurchased, and amountPurchased exactly.
- Then a "Key Takeaway" paragraph identifying a real trend in THIS list, tied to the actual items.
SECTION 3 — Product Mix Analysis
- If productMix is empty or absent (no category rows), there is no current-season purchasing to
  analyze: say so plainly, do NOT claim the mix is "well-balanced," and do not name any strength or
  opportunity. Skip the rest of this section.
- Explain you're comparing their mix to the anonymous regional average AND to their own prior year.
  All percentages are share of DOLLARS SPENT, not absolute sales. A low share does NOT mean they
  sell little in a category — only that it's a smaller slice of their mix. Never say a category is
  one they "don't sell," are "weak in," or "underperform" based on share alone.
- STANDOUT STRENGTH: a category where retailerPercentage exceeds regionalPercentage by at least 3
  points OR is at least 1.5x the regional figure. Call out their single most distinctive strength
  (largest gap by ratio) and mention the largest gap by absolute points if different.
- GROWTH OPPORTUNITY: a category where retailerPercentage is at least 3 points BELOW
  regionalPercentage. Name their biggest one. If NO category is at least 3 points below regional,
  state plainly that their mix is well-balanced with no significant gaps versus the region, and do
  NOT invent or force an opportunity.
- Cross-check Section 2 against Section 3: if a flagged opportunity category ALSO contains one of
  their top sellers, acknowledge that success and frame it as EXPANDING a proven category, naming
  the existing seller. Never imply they are absent or failing where they have a bestseller.
- Where retailerPreviousPercentage is available, note any category that moved ~3+ points versus
  last year (up or down) — the most actionable signal.
- Write a short "Analysis" paragraph on what the mix suggests about their customer base and market
  position.
SECTION 4 — Three Data-Driven Recommendations
Give EXACTLY three specific, actionable recommendations, each explicitly tied to the data above.
CATEGORY RULE (critical): when you name products to add, use ONLY items from recommendableProducts,
and the item's "category" field must EXACTLY equal the category of that recommendation. Categories
that look similar are still DIFFERENT — e.g. "500 GRAM MULTI-EFFECT", "500 GRAM COMPOUND CAKE", and
"500 GRAM MULTI-EFFECT FOUNTAINS" are three separate categories; never borrow an item from one for
another. If recommendableProducts has no item whose category exactly matches, recommend a product
TYPE instead and say so — never substitute an item from a different category.
NAME RULE: quote each recommended item's FULL name EXACTLY as it appears in recommendableProducts —
the item number and the product name must come from the SAME entry. Never pair an item number with
a different product's name.
1. Double down on their standout strength from Section 3: advise expanding THAT exact category and
   name 1-2 items from recommendableProducts whose category exactly matches it.
2. Capture their biggest growth opportunity from Section 3 and name 1-2 items from
   recommendableProducts whose category exactly matches it. If that category holds a top seller,
   position the items as broadening a proven lineup and name that seller. If Section 3 found NO
   qualifying opportunity, instead give a second optimization/upsell tied to a strength or a top
   seller.
3. A smart optimization or upsell — e.g., a "good / better / best" ladder within a category that
   already sells well for them, or a complement to a Section 2 top seller (named exactly).
CARRYOVER CAVEAT (after Section 4, brief): one or two sentences noting that product carried over
from the prior season can affect what these numbers show, so this review should be read alongside
their own on-hand inventory.
CLOSE: A short, warm closing that uses "we"/"our" to reinforce the partnership and looks ahead to
next season.
DATA:
<JSON from dbo.CustomerSummaryJSON here>
""";
    }
}
