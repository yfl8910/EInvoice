using EInvoice.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EInvoice.Web.Services;

public class InvoiceStore(IDbContextFactory<InvoiceDb> factory, InvoiceParser parser)
{
    readonly SemaphoreSlim writes = new(1, 1);
    public async Task<(int Id, bool Duplicate)> Import(byte[] bytes, string name)
    {
        var invoice = parser.Parse(bytes, name);
        await writes.WaitAsync();
        try {
            await using var db = await factory.CreateDbContextAsync();
            var existing = await db.Invoices.SingleOrDefaultAsync(x => x.Sha256 == invoice.Sha256);
            if (existing != null) return (existing.Id, true);
            if (await db.Invoices.AnyAsync(x => x.Number == invoice.Number && x.Supplier == invoice.Supplier))
                invoice.Checks += "\nWarning: another file has the same supplier and invoice number. Review possible duplicate or correction.";
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            return (invoice.Id, false);
        } finally { writes.Release(); }
    }
    public async Task<string> Validate(Invoice invoice)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (await db.Invoices.AnyAsync(x => x.Sha256 == invoice.Sha256))
            return "Already imported. Saving will return the existing invoice without creating a duplicate.";
        if (await db.Invoices.AnyAsync(x => x.Number == invoice.Number && x.Supplier == invoice.Supplier))
            return "Warning: another file has the same supplier and invoice number. Review possible duplicate or correction before saving.";
        return "No duplicate found in saved invoices.";
    }
    public async Task<List<Invoice>> List()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Invoices.AsNoTracking().OrderByDescending(x => x.Id).Select(x => new Invoice {
            Id = x.Id, Number = x.Number, Supplier = x.Supplier, Buyer = x.Buyer, IssueDate = x.IssueDate,
            Currency = x.Currency, Net = x.Net, Tax = x.Tax, Payable = x.Payable
        }).ToListAsync();
    }
    public async Task<Invoice?> Get(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Invoices.AsNoTracking().Include(x => x.Lines).Include(x => x.Fields).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id);
    }
}
