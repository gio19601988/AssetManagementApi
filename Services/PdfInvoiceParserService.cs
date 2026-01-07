using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;

namespace AssetManagementApi.Services;

public class PdfInvoiceParserService
{
    public class ParsedInvoice
    {
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? SellerName { get; set; }
        public string? SellerInn { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerInn { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
    }

    public class InvoiceItem
    {
        public string? ProductName { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
    }

    public ParsedInvoice ParseInvoice(Stream pdfStream)
    {
        var result = new ParsedInvoice();

        using (PdfDocument document = PdfDocument.Open(pdfStream))
        {
            var fullText = string.Join("\n", document.GetPages().Select(p => p.Text));
            fullText = Regex.Replace(fullText, @"\s+", " "); // Normalize spaces

            // ზედდებულის ნომერი (უფრო ფლექსიბლური)
            var invoiceNumMatch = Regex.Match(fullText, @"ზედდებული\s*№?\s*([\w\-]+)", RegexOptions.IgnoreCase);
            if (invoiceNumMatch.Success)
                result.InvoiceNumber = invoiceNumMatch.Groups[1].Value.Trim();

            // თარიღი (გავაუმჯობესე: optional label, მხოლოდ date ან date+time)
            var dateMatch = Regex.Match(fullText, @"(?:თარიღი|დრო)?\s*:?\s*(\d{2}\.\d{2}\.\d{4})(?:\s*(?:დრო|სთ|გიო)?\.?\s*(\d{2}:\d{2}))?", RegexOptions.IgnoreCase);
            if (dateMatch.Success && DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                result.InvoiceDate = date;

            // გამყიდველი
            var sellerMatch = Regex.Match(fullText, @"გამყიდველი\s*:?\s*([^\n\r]+)", RegexOptions.IgnoreCase);
            if (sellerMatch.Success)
                result.SellerName = sellerMatch.Groups[1].Value.Trim();

            // საიდენტიფიკაციო ნომერი (გამყიდველი)
            var sellerInnMatch = Regex.Match(fullText, @"საიდენტიფიკაციო\s+ნომერი\s*:?\s*(\d+)", RegexOptions.IgnoreCase);
            if (sellerInnMatch.Success)
                result.SellerInn = sellerInnMatch.Groups[1].Value.Trim();

            // ცხრილის ამოკითხვა
            var lines = fullText.Split('\n').Select(l => Regex.Replace(l.Trim(), @"\s+", " ")).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            bool inTable = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // ცხრილის დაწყება (უფრო ფლექსიბლური header დადგენა)
                if (Regex.IsMatch(line, @"საქონლის\s+დასახელება|რაოდენობა|ერთეულის\s+ფასი|სულ", RegexOptions.IgnoreCase))
                {
                    inTable = true;
                    continue;
                }

                // ცხრილის დასასრული
                if (inTable && Regex.IsMatch(line, @"სულ|ჯამი|გადასახდელი\s+თანხა", RegexOptions.IgnoreCase))
                {
                    break;
                }

                if (inTable)
                {
                    // უფრო ფლექსიბლური regex: optional row#, optional code, more space tolerance
                    var itemMatch = Regex.Match(
                        line,
                        @"^(\d+\s*)?(.+?)\s+(\d{6,10})?\s*(\w+)?\s+([\d.,]+)\s+([\d.,]+)\s+([\d.,]+)$",
                        RegexOptions.IgnoreCase);

                    if (itemMatch.Success)
                    {
                        result.Items.Add(new InvoiceItem
                        {
                            ProductName = itemMatch.Groups[2].Value.Trim(),
                            Quantity = ParseDecimal(itemMatch.Groups[5].Value),
                            UnitPrice = ParseDecimal(itemMatch.Groups[6].Value),
                            TotalPrice = ParseDecimal(itemMatch.Groups[7].Value),
                        });
                    }
                    else if (i + 1 < lines.Count) // Multi-line name
                    {
                        var combinedLine = line + " " + lines[i + 1];
                        combinedLine = Regex.Replace(combinedLine, @"\s+", " ");
                        itemMatch = Regex.Match(
                            combinedLine,
                            @"^(\d+\s*)?(.+?)\s+(\d{6,10})?\s*(\w+)?\s+([\d.,]+)\s+([\d.,]+)\s+([\d.,]+)$",
                            RegexOptions.IgnoreCase);

                        if (itemMatch.Success)
                        {
                            result.Items.Add(new InvoiceItem
                            {
                                ProductName = itemMatch.Groups[2].Value.Trim(),
                                Quantity = ParseDecimal(itemMatch.Groups[5].Value),
                                UnitPrice = ParseDecimal(itemMatch.Groups[6].Value),
                                TotalPrice = ParseDecimal(itemMatch.Groups[7].Value),
                            });
                            i++;
                        }
                    }
                }
            }
        }

        return result;
    }

    private decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }
}