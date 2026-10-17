using EInvoice.Web.Data;
using PdfSharp.Pdf.IO;

namespace EInvoice.Web.Services;

public static class PdfVerification
{
    public static void Run(IServiceProvider services, string root)
    {
        var parser = services.GetRequiredService<InvoiceParser>();
        var pdf = services.GetRequiredService<InvoicePdf>();
        var output = Path.GetFullPath(Path.Combine(root, "..", "tmp", "pdfs"));
        Directory.CreateDirectory(output);
        var files = Directory.GetFiles(Path.Combine(root, "..", "电子发票"), "*.xml");
        foreach (var file in files) {
            var invoice = parser.Parse(File.ReadAllBytes(file), Path.GetFileName(file));
            var bytes = pdf.Generate(invoice);
            using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
            if (document.PageCount == 0) throw new InvalidOperationException("Empty PDF.");
            File.WriteAllBytes(Path.Combine(output, invoice.Number + ".pdf"), bytes);
        }
        var longInvoice = parser.Parse(File.ReadAllBytes(files[0]), "long-invoice.xml");
        longInvoice.Number = "PDF-TEST-ÄÖÜ-ß";
        longInvoice.Supplier = "Müller & Söhne GmbH";
        longInvoice.Note = string.Join(" ", Enumerable.Repeat("Long note with German characters: ÄÖÜ äöü ß.", 30));
        var original = longInvoice.Lines[0];
        longInvoice.Lines = Enumerable.Range(1, 120).Select(i => new InvoiceLine {
            Id = i, Number = i.ToString(), Name = "Long item description with multiple words for wrapping and German text: Größe für Zubehör",
            ItemCode = original.ItemCode, Scheme = original.Scheme, Quantity = original.Quantity, Unit = original.Unit,
            Price = original.Price, Net = original.Net, TaxCategory = original.TaxCategory, TaxRate = original.TaxRate
        }).ToList();
        longInvoice.Net = longInvoice.Lines.Sum(x => x.Net);
        longInvoice.Tax = decimal.Round(longInvoice.Net * original.TaxRate / 100, 2);
        longInvoice.Payable = longInvoice.Net + longInvoice.Tax;
        var longBytes = pdf.Generate(longInvoice);
        using var longDocument = PdfReader.Open(new MemoryStream(longBytes), PdfDocumentOpenMode.Import);
        if (longDocument.PageCount < 3) throw new InvalidOperationException("Pagination not exercised.");
        File.WriteAllBytes(Path.Combine(output, "long-invoice.pdf"), longBytes);
        Console.WriteLine($"PASS: {files.Length} sample PDFs and a {longDocument.PageCount}-page invoice generated in {output}");
    }
}
