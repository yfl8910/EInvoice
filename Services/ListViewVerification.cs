using EInvoice.Web.Components;
using EInvoice.Web.Components.Pages;
using EInvoice.Web.Components.Import;
using EInvoice.Web.Components.Analysis;
using EInvoice.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EInvoice.Web.Services;

public static class ListViewVerification
{
    public static async Task Run(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var invoices = await scope.ServiceProvider.GetRequiredService<InvoiceStore>().List();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () => {
            var page = await renderer.RenderComponentAsync<Home>();
            var html = page.ToHtmlString();
            foreach (var invoice in invoices.Take(25))
                if (!html.Contains($"href=\"/invoices/{invoice.Id}\""))
                    throw new InvalidOperationException("Invoice List did not render a saved invoice with an empty search.");

            var rows = new List<Invoice> {
                new() { Id = 101, Number = "INV-ALPHA", Supplier = "Supplier A" },
                new() { Id = 102, Number = "INV-BETA", Supplier = "Supplier B" }
            };
            async Task<string> Render(string search) => (await renderer.RenderComponentAsync<InvoiceListTable>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(InvoiceListTable.Invoices)] = rows, [nameof(InvoiceListTable.Search)] = search }))).ToHtmlString();
            var filtered = await Render("alpha");
            if (!filtered.Contains("/invoices/101\"") || filtered.Contains("/invoices/102\""))
                throw new InvalidOperationException("Invoice table search did not filter correctly.");
            if (!(await Render("missing-invoice")).Contains("No invoices found."))
                throw new InvalidOperationException("Invoice table empty-search-results message missing.");
            var cleared = await Render("");
            if (!cleared.Contains("/invoices/101\"") || !cleared.Contains("/invoices/102\""))
                throw new InvalidOperationException("Clearing search did not show all invoices.");

            var previewInvoice = new Invoice { Number = "PREVIEW-TEST", FileName = "preview.xml", Checks = "CHECK-MARKER", Payable = 123.45m, Currency = "EUR" };
            async Task<string> Preview(bool showChecks) => (await renderer.RenderComponentAsync<InvoicePreview>(ParameterView.FromDictionary(
                new Dictionary<string, object?> {
                    [nameof(InvoicePreview.Invoice)] = previewInvoice,
                    [nameof(InvoicePreview.ShowChecks)] = showChecks,
                    [nameof(InvoicePreview.ValidationMessage)] = "VALIDATION-MARKER"
                }))).ToHtmlString();
            var unvalidated = await Preview(false);
            if (!unvalidated.Contains("PREVIEW-TEST") || !unvalidated.Contains("preview.xml") || !unvalidated.Contains("123.45 EUR") ||
                unvalidated.Contains("CHECK-MARKER") || unvalidated.Contains("VALIDATION-MARKER"))
                throw new InvalidOperationException("Upload preview content or validation visibility incorrect.");
            var validated = await Preview(true);
            if (!validated.Contains("CHECK-MARKER") || !validated.Contains("VALIDATION-MARKER"))
                throw new InvalidOperationException("Validated preview did not display check results.");
            if (invoices.Count > 0) {
                var id = invoices[0].Id;
                var detail = (await renderer.RenderComponentAsync<InvoiceDetail>(ParameterView.FromDictionary(
                    new Dictionary<string, object?> { [nameof(InvoiceDetail.Id)] = id }))).ToHtmlString();
                if (!detail.Contains("class=\"invoice-preview\"") || !detail.Contains($"href=\"/invoices/{id}/pdf\"") || !detail.Contains($"href=\"/invoices/{id}/original\""))
                    throw new InvalidOperationException("Detail page did not render the shared preview and download actions.");
                if (!detail.Contains("Additional invoice details") || !detail.Contains("Actual delivery date (XML)") || !detail.Contains("Tax breakdown"))
                    throw new InvalidOperationException("Expanded XML fields did not render for a stored invoice.");
            }
            var analysis = (await renderer.RenderComponentAsync<InvoiceAnalysisPage>()).ToHtmlString();
            if (!analysis.Contains("Group by") || !analysis.Contains("Currency totals") || !analysis.Contains("Supplier") || !analysis.Contains("Payable"))
                throw new InvalidOperationException("Analysis page did not render its filter, summary and table components.");
            var buyerRows = new List<InvoiceSummary> { new("HiddenSupplier", "VisibleBuyer", "EUR", 2, 100m, 19m, 119m) };
            var buyerTable = (await renderer.RenderComponentAsync<InvoiceAnalysisTable>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(InvoiceAnalysisTable.Rows)] = buyerRows, [nameof(InvoiceAnalysisTable.Dimension)] = "Buyer" }))).ToHtmlString();
            if (!buyerTable.Contains("VisibleBuyer") || buyerTable.Contains("HiddenSupplier") || !buyerTable.Contains("119.00"))
                throw new InvalidOperationException("Buyer summary table content incorrect.");
            var uploadPage = (await renderer.RenderComponentAsync<InvoiceImport>()).ToHtmlString();
            if (!uploadPage.Contains("aria-controls=\"invoice-upload-panel\"") || !uploadPage.Contains("aria-expanded=\"false\"") || !uploadPage.Contains("Validation results") || uploadPage.Contains("type=\"file\""))
                throw new InvalidOperationException("Upload page should initially show its Upload button with the file panel collapsed.");
            var uploadPanel = (await renderer.RenderComponentAsync<InvoiceUploadPanel>()).ToHtmlString();
            if (!uploadPanel.Contains("Upload XML files") || !uploadPanel.Contains("type=\"file\""))
                throw new InvalidOperationException("Expanded upload panel did not render its file input.");
            var pending = new List<PendingInvoice> {
                new() { Name = "saved.xml", Invoice = previewInvoice, Validated = true, SavedId = 101 },
                new() { Name = "broken.xml", Error = "Invalid XML" }
            };
            var fileList = (await renderer.RenderComponentAsync<UploadedInvoiceList>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(UploadedInvoiceList.Items)] = pending, [nameof(UploadedInvoiceList.Selected)] = pending[0] }))).ToHtmlString();
            if (!fileList.Contains("/invoices/101") || !fileList.Contains("Invalid XML") || !fileList.Contains("aria-pressed=\"true\""))
                throw new InvalidOperationException("Uploaded file status, selection or saved link missing.");
        });
        Console.WriteLine("PASS: Invoice List page renders saved invoices; table search, no matches and cleared search verified.");
        Console.WriteLine("PASS: shared invoice preview, validation visibility and detail download actions rendered.");
        Console.WriteLine("PASS: analysis filters/cards/table, upload panel/dialog and uploaded file selection/status components rendered.");
    }
}
