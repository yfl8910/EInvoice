using EInvoice.Web.Data;

namespace EInvoice.Web.Services;

public static class InvoiceAnalysis
{
    public static List<InvoiceSummary> Summarize(IEnumerable<Invoice> invoices, string dimension) => invoices
        .GroupBy(x => (Supplier: dimension is "Supplier" or "SupplierBuyer" ? Label(x.Supplier) : "",
            Buyer: dimension is "Buyer" or "SupplierBuyer" ? Label(x.Buyer) : "",
            Currency: Label(x.Currency)))
        .Select(g => new InvoiceSummary(g.Key.Supplier, g.Key.Buyer, g.Key.Currency,
            g.Count(), g.Sum(x => x.Net), g.Sum(x => x.Tax), g.Sum(x => x.Payable)))
        .OrderBy(x => x.Currency).ThenByDescending(x => x.Payable).ThenBy(x => x.Supplier).ThenBy(x => x.Buyer).ToList();

    static string Label(string value) => string.IsNullOrWhiteSpace(value) ? "(Not provided)" : value;
}

public record InvoiceSummary(string Supplier, string Buyer, string Currency, int Count, decimal Net, decimal Tax, decimal Payable);
