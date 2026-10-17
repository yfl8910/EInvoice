using System.Text;
using System.Xml;
using EInvoice.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EInvoice.Web.Services;

public static class SampleVerification
{
    public static async Task Run(IServiceProvider services, string root)
    {
        var folder = Path.GetFullPath(Path.Combine(root, "..", "电子发票"));
        var store = services.GetRequiredService<InvoiceStore>();
        var parser = services.GetRequiredService<InvoiceParser>();
        var expected = new Dictionary<string, (int Lines, decimal Net, decimal Tax, decimal Payable)> {
            ["6250035639"] = (1, 40645.60m, 7722.66m, 48368.26m),
            ["6250035730"] = (1, 4492m, 853.48m, 5345.48m),
            ["6250035731"] = (2, 10818.30m, 2055.48m, 12873.78m),
            ["6250035732"] = (1, 5485.30m, 1042.21m, 6527.51m),
            ["6250035733"] = (1, 4616.44m, 877.12m, 5493.56m),
            ["6250035734"] = (2, 5304.68m, 1007.89m, 6312.57m)
        };
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        var sortingRows = new List<Invoice> {
            new() { Id = 1, Number = "C", Supplier = "charlie", Buyer = "Alpha", Currency = "USD", IssueDate = "2026-10-03", Net = 100m, Tax = 19m, Payable = 119m },
            new() { Id = 2, Number = "A", Supplier = "Alpha", Buyer = "charlie", Currency = "EUR", IssueDate = "2026-01-01", Net = -5m, Tax = -0.95m, Payable = -5.95m },
            new() { Id = 3, Number = "B", Supplier = "Bravo", Buyer = "Bravo", Currency = "GBP", IssueDate = "2026-05-01", Net = 20m, Tax = 3.8m, Payable = 23.8m }
        };
        foreach (var column in Enum.GetValues<InvoiceSortColumn>()) {
            int[] ascending = column == InvoiceSortColumn.Buyer ? [1, 3, 2] : [2, 3, 1];
            Check(InvoiceListSorting.Sort(sortingRows, column, false).Select(x => x.Id).SequenceEqual(ascending), $"Ascending sort failed: {column}");
            Check(InvoiceListSorting.Sort(sortingRows, column, true).Select(x => x.Id).SequenceEqual(ascending.Reverse()), $"Descending sort failed: {column}");
        }
        Check(InvoiceListSorting.Sort(sortingRows, null, false).Select(x => x.Id).SequenceEqual([3, 2, 1]), "Default newest-first order changed.");
        Check(!InvoiceListSorting.Sort([], InvoiceSortColumn.Net, false).Any(), "Empty list sorting failed.");
        var equalAmounts = new[] { new Invoice { Id = 3, Net = 10m }, new Invoice { Id = 1, Net = 10m } };
        Check(InvoiceListSorting.Sort(equalAmounts, InvoiceSortColumn.Net, true).Select(x => x.Id).SequenceEqual([1, 3]), "Equal amounts need a stable order for pagination.");
        Console.WriteLine("PASS: all eight invoice columns sort ascending and descending; numeric amounts, dates, text, ties and default order verified.");
        var analysisRows = new List<Invoice> {
            new() { Supplier = "A", Buyer = "X", Currency = "EUR", Net = 100m, Tax = 19m, Payable = 119m },
            new() { Supplier = "A", Buyer = "Y", Currency = "EUR", Net = -20m, Tax = -3.8m, Payable = -23.8m },
            new() { Supplier = "A", Buyer = "X", Currency = "USD", Net = 50m, Tax = 5m, Payable = 55m }
        };
        var supplierSummary = InvoiceAnalysis.Summarize(analysisRows, "Supplier");
        Check(supplierSummary.Count == 2, "Analysis mixed currencies.");
        var eur = supplierSummary.Single(x => x.Currency == "EUR");
        Check((eur.Count, eur.Net, eur.Tax, eur.Payable) == (2, 80m, 15.2m, 95.2m), "Analysis totals or negative amounts are incorrect.");
        Check(InvoiceAnalysis.Summarize(analysisRows, "Buyer").Count == 3, "Buyer grouping incorrect.");
        Check(InvoiceAnalysis.Summarize(analysisRows, "SupplierBuyer").Count == 3, "Supplier / buyer grouping incorrect.");
        Check(InvoiceAnalysis.Summarize([], "Supplier").Count == 0, "Empty analysis incorrect.");
        Console.WriteLine("PASS: analysis dimensions, separate currencies, invoice counts, negative amounts and totals.");
        var filter = new InvoiceAnalysisFilter { Search = "a", Currency = "EUR", DateFrom = new(2026, 1, 1), DateTo = new(2026, 1, 31) };
        Check(filter.Matches(new Invoice { Supplier = "A", Currency = "EUR", IssueDate = "2026-01-01" }) &&
            filter.Matches(new Invoice { Buyer = "A", Currency = "EUR", IssueDate = "2026-01-31" }), "Analysis date boundaries changed.");
        Check(!filter.Matches(new Invoice { Supplier = "A", Currency = "USD", IssueDate = "2026-01-01" }) &&
            !filter.Matches(new Invoice { Supplier = "A", Currency = "EUR", IssueDate = "invalid" }), "Currency or invalid-date filtering changed.");
        Check((filter with { DateTo = new(2025, 12, 31) }).InvalidDateRange, "Invalid date range not detected.");
        Check(new InvoiceAnalysisFilter().Matches(new Invoice { IssueDate = "" }), "Reset filters should include invoices without dates.");
        var files = Directory.GetFiles(folder, "*.xml");
        Check(files.Length == 6, "Expected six sample files.");
        var ids = new HashSet<int>();
        foreach (var file in files) {
            var bytes = await File.ReadAllBytesAsync(file);
            var beforePreview = (await store.List()).Count;
            var preview = parser.Parse(bytes, Path.GetFileName(file));
            var validation = await store.Validate(preview);
            Check(preview.Id == 0 && (await store.List()).Count == beforePreview, "Preview or validation saved an invoice.");
            Check(!string.IsNullOrWhiteSpace(validation), "Validation result missing.");
            var result = await store.Import(bytes, Path.GetFileName(file));
            var invoice = (await store.Get(result.Id))!;
            var e = expected[invoice.Number];
            Check((invoice.Lines.Count, invoice.Net, invoice.Tax, invoice.Payable) == e, "Stored values differ: " + file);
            Check(invoice.OriginalXml.SequenceEqual(bytes), "Original bytes changed.");
            Check(invoice.Lines.All(x => x.Number.StartsWith("0000") && x.Scheme == "0160" && x.Unit == "EA"), "Line attributes lost.");
            Check(invoice.Fields.Any(x => x.Path.Contains("DeliveryLocation")), "Delivery address missing.");
            Check(invoice.PaymentAccount.StartsWith("IBAN "), "Original account text lost.");
            Check((await store.Import(bytes, "renamed.xml")).Duplicate, "Duplicate file was not detected.");
            Check((await store.Validate(preview)).StartsWith("Already imported"), "Validation did not detect saved duplicate.");
            var renamed = Encoding.UTF8.GetString(bytes).Replace("xmlns:cbc=", "xmlns:basic=").Replace("cbc:", "basic:");
            Check(parser.Parse(Encoding.UTF8.GetBytes(renamed), "renamed.xml").Number == invoice.Number, "Prefix-independent parsing failed.");
            ids.Add(invoice.Id);
            Console.WriteLine($"PASS {invoice.Number}: {invoice.Lines.Count} lines, {invoice.Payable} {invoice.Currency}, database round-trip and duplicate detection");
        }
        Check(ids.Count == 6, "Expected six distinct invoices.");
        foreach (var invalid in new[] { "<broken", "<!DOCTYPE Invoice [<!ENTITY x SYSTEM 'file:///secret'>]><Invoice>&x;</Invoice>" }) {
            var rejected = false;
            try { parser.Parse(Encoding.UTF8.GetBytes(invalid), "invalid.xml"); }
            catch (Exception ex) when (ex is XmlException or InvalidDataException) { rejected = true; }
            Check(rejected, "Unsafe or malformed XML was accepted.");
        }
        // Use a fresh context to prove persisted child records survived the import contexts.
        await using var db = await services.GetRequiredService<IDbContextFactory<InvoiceDb>>().CreateDbContextAsync();
        Check(await db.Set<InvoiceLine>().CountAsync(x => ids.Contains(x.InvoiceId)) == 8, "Expected eight persisted lines.");
        await ListViewVerification.Run(services);
        Console.WriteLine("PASS: 6 invoices / 8 lines persisted; original bytes, attributes, alternate prefix, malformed XML and DTD rejection verified.");
    }
}
