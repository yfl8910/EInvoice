using System.Globalization;
using EInvoice.Web.Data;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace EInvoice.Web.Services;

public sealed class InvoicePdf
{
    public InvoicePdf()
    {
        GlobalFontSettings.FontResolver = new InvoiceFontResolver();
    }

    public byte[] Generate(Invoice invoice)
    {
        var document = new Document();
        document.Info.Title = $"Invoice {invoice.Number}";
        document.Info.Author = "EInvoice";
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = "InvoiceSans";
        normal.Font.Size = 9;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.Font.Size = 8;
        footer.AddText("EInvoice | Page "); footer.AddPageField(); footer.AddText(" of "); footer.AddNumPagesField();
        var title = section.AddParagraph($"Invoice {invoice.Number}");
        title.Format.Font.Size = 22; title.Format.Font.Bold = true;
        title.Format.Font.Color = Color.FromRgb(20, 42, 67);
        section.AddParagraph("System-generated PDF preview of the saved XML invoice.");
        var info = section.AddTable();
        info.AddColumn(Unit.FromCentimeter(4)); info.AddColumn(Unit.FromCentimeter(13.4));
        void Field(string label, string value) {
            var row = info.AddRow();
            row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
            row.Cells[1].AddParagraph(string.IsNullOrEmpty(value) ? "-" : value);
        }
        Field("Supplier", invoice.Supplier); Field("Buyer", invoice.Buyer);
        Field("Issue date", invoice.IssueDate); Field("Due date", invoice.DueDate);
        Field("Buyer reference", invoice.BuyerReference); Field("Order reference", invoice.OrderReference);
        Field("Despatch reference", invoice.DespatchReference); Field("Payment account", invoice.PaymentAccount);
        Field("Currency", invoice.Currency);
        if (!string.IsNullOrEmpty(invoice.Note)) Field("Note", invoice.Note);
        foreach (var detail in InvoiceDetailProjection.Build(invoice)) {
            var detailHeading = section.AddParagraph(detail.Title);
            detailHeading.Format.SpaceBefore = Unit.FromPoint(10);
            detailHeading.Format.Font.Bold = true;
            detailHeading.Format.KeepWithNext = true;
            var detailTable = section.AddTable();
            detailTable.AddColumn(Unit.FromCentimeter(5));
            detailTable.AddColumn(Unit.FromCentimeter(12.4));
            foreach (var entry in detail.Entries) {
                var row = detailTable.AddRow();
                row.Cells[0].AddParagraph(entry.Label).Format.Font.Bold = true;
                row.Cells[1].AddParagraph(entry.Value);
            }
        }
        var heading = section.AddParagraph("Invoice lines");
        heading.Format.SpaceBefore = Unit.FromPoint(16); heading.Format.Font.Size = 13; heading.Format.Font.Bold = true;
        var lines = section.AddTable();
        foreach (var width in new[] { 1.3, 5.1, 2.4, 2.6, 3.0, 3.0 }) lines.AddColumn(Unit.FromCentimeter(width));
        lines.Borders.Width = 0.4; lines.Borders.Color = Colors.LightGray;
        lines.TopPadding = Unit.FromPoint(5); lines.BottomPadding = Unit.FromPoint(5);
        var header = lines.AddRow(); header.HeadingFormat = true;
        header.Shading.Color = Color.FromRgb(232, 239, 247); header.Format.Font.Bold = true;
        var labels = new[] { "Line", "Item / code", "Quantity", "Unit price", "Net", "Tax" };
        for (var i = 0; i < labels.Length; i++) header.Cells[i].AddParagraph(labels[i]);
        foreach (var line in invoice.Lines.OrderBy(x => x.Id)) {
            var row = lines.AddRow();
            var values = new[] { line.Number, line.Name + (line.ItemCode.Length > 0 ? $"\n{line.ItemCode} / {line.Scheme}" : ""),
                $"{Number(line.Quantity)} {line.Unit}", Number(line.Price), Money(line.Net), $"{line.TaxCategory} / {Number(line.TaxRate)}%" };
            for (var i = 0; i < values.Length; i++) {
                row.Cells[i].AddParagraph(values[i]);
                if (i >= 2) row.Cells[i].Format.Alignment = ParagraphAlignment.Right;
            }
        }
        var totals = section.AddParagraph($"Net: {Money(invoice.Net)} {invoice.Currency}\nTax: {Money(invoice.Tax)} {invoice.Currency}\nPayable: {Money(invoice.Payable)} {invoice.Currency}");
        totals.Format.SpaceBefore = Unit.FromPoint(12); totals.Format.Alignment = ParagraphAlignment.Right; totals.Format.Font.Bold = true;
        var checks = section.AddParagraph("Checks\n" + invoice.Checks);
        checks.Format.SpaceBefore = Unit.FromPoint(12); checks.Format.Font.Size = 8;
        section.AddParagraph("Original file: " + invoice.FileName).Format.Font.Size = 8;
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var pdfDocument = renderer.PdfDocument;
        using var output = new MemoryStream();
        pdfDocument.Save(output, false);
        return output.ToArray();
    }

    static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    static string Number(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);

    sealed class InvoiceFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(bold ? "bold" : "regular");
        public byte[] GetFont(string faceName)
        {
            var bold = faceName == "bold";
            var path = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), bold ? "arialbd.ttf" : "arial.ttf")
                : "/usr/share/fonts/truetype/dejavu/" + (bold ? "DejaVuSans-Bold.ttf" : "DejaVuSans.ttf");
            return File.ReadAllBytes(path);
        }
    }
}
