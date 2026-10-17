using System.Globalization;
using EInvoice.Web.Data;

namespace EInvoice.Web.Services;

public enum InvoiceSortColumn { Invoice, Date, Supplier, Buyer, Currency, Net, Tax, Payable }

public static class InvoiceListSorting
{
    public static IEnumerable<Invoice> Sort(IEnumerable<Invoice> invoices, InvoiceSortColumn? column, bool descending)
    {
        return column switch {
            InvoiceSortColumn.Invoice => Order(invoices, x => x.Number, descending, StringComparer.OrdinalIgnoreCase),
            InvoiceSortColumn.Date => Order(invoices, x => ParseDate(x.IssueDate), descending),
            InvoiceSortColumn.Supplier => Order(invoices, x => x.Supplier, descending, StringComparer.OrdinalIgnoreCase),
            InvoiceSortColumn.Buyer => Order(invoices, x => x.Buyer, descending, StringComparer.OrdinalIgnoreCase),
            InvoiceSortColumn.Currency => Order(invoices, x => x.Currency, descending, StringComparer.OrdinalIgnoreCase),
            InvoiceSortColumn.Net => Order(invoices, x => x.Net, descending),
            InvoiceSortColumn.Tax => Order(invoices, x => x.Tax, descending),
            InvoiceSortColumn.Payable => Order(invoices, x => x.Payable, descending),
            _ => invoices.OrderByDescending(x => x.Id)
        };
    }

    static DateOnly? ParseDate(string value) => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var date) ? date : null;

    static IOrderedEnumerable<Invoice> Order<TKey>(IEnumerable<Invoice> invoices, Func<Invoice, TKey> key, bool descending, IComparer<TKey>? comparer = null) =>
        (descending ? invoices.OrderByDescending(key, comparer) : invoices.OrderBy(key, comparer)).ThenBy(x => x.Id);
}
