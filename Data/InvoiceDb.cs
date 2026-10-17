using Microsoft.EntityFrameworkCore;

namespace EInvoice.Web.Data;

public class InvoiceDb(DbContextOptions<InvoiceDb> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Invoice>().HasIndex(x => x.Sha256).IsUnique();
        model.Entity<Invoice>().HasIndex(x => x.Number);
        model.Entity<Invoice>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InvoiceId);
        model.Entity<Invoice>().HasMany(x => x.Fields).WithOne().HasForeignKey(x => x.InvoiceId);
    }
}
