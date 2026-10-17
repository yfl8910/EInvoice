using System.Globalization;
namespace EInvoice.Web.Data;

public sealed record InvoiceAnalysisFilter
{
    public string Dimension { get; init; } = "Supplier";
    public string Search { get; init; } = "";
    public string Currency { get; init; } = "";
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public bool InvalidDateRange => DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo;
    public bool Matches(Invoice invoice) =>
        (invoice.Supplier.Contains(Search, StringComparison.OrdinalIgnoreCase) || invoice.Buyer.Contains(Search, StringComparison.OrdinalIgnoreCase)) &&
        (Currency.Length == 0 || invoice.Currency == Currency) && MatchesDate(invoice.IssueDate);
    bool MatchesDate(string value) => !DateFrom.HasValue && !DateTo.HasValue ||
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
        (!DateFrom.HasValue || date >= DateFrom.Value.Date) && (!DateTo.HasValue || date <= DateTo.Value.Date);
}
