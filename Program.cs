using EInvoice.Web.Components;
using EInvoice.Web.Data;
using EInvoice.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// Build output also needs its static asset manifest when started outside Development.
builder.WebHost.UseStaticWebAssets();
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
builder.Services.AddDbContextFactory<InvoiceDb>(options => options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "invoices.db")};Default Timeout=15"));
builder.Services.AddSingleton<InvoiceParser>();
builder.Services.AddSingleton<InvoiceStore>();
builder.Services.AddSingleton<InvoicePdf>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
if (args.Contains("--verify-pdf")) {
    PdfVerification.Run(app.Services, builder.Environment.ContentRootPath);
    return;
}
await using (var db = await app.Services.GetRequiredService<IDbContextFactory<InvoiceDb>>().CreateDbContextAsync()) {
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

if (args.Contains("--verify-samples")) {
    await SampleVerification.Run(app.Services, builder.Environment.ContentRootPath);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/invoices/{id:int}/original", async (int id, InvoiceStore store) => {
    var invoice = await store.Get(id);
    return invoice is null ? Results.NotFound() : Results.File(invoice.OriginalXml, "application/xml", invoice.FileName);
});
app.MapGet("/invoices/{id:int}/pdf", async (int id, InvoiceStore store, InvoicePdf pdf) => {
    var invoice = await store.Get(id);
    return invoice is null ? Results.Text("Invoice not found.", statusCode: 404) : Results.File(pdf.Generate(invoice), "application/pdf", $"invoice-{id}.pdf");
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
