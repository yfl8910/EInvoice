using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using EInvoice.Web.Data;

namespace EInvoice.Web.Services;

public class InvoiceParser
{
    public const int MaxBytes = 30 * 1024 * 1024;
    static readonly XNamespace I = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    static readonly XNamespace A = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    static readonly XNamespace B = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    static string Text(XElement? root, XName name) => root?.Element(name)?.Value ?? "";
    static decimal Amount(XElement? root, XName name) => decimal.Parse(Text(root, name), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    public Invoice Parse(byte[] bytes, string fileName)
    {
        if (bytes.Length == 0 || bytes.Length > MaxBytes) throw new InvalidDataException("The file must contain XML and be no larger than 30 MB.");
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxBytes });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing XML root.");
        if (root.Name != I + "Invoice") throw new InvalidDataException("Only UBL Invoice XML is supported by this prototype.");
        var custom = Text(root, B + "CustomizationID");
        if (custom != "urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_3.0")
            throw new InvalidDataException("Unsupported customization. This prototype supports XRechnung 3.0 UBL samples.");
        var totals = root.Element(A + "LegalMonetaryTotal");
        var account = root.Element(A + "PaymentMeans")?.Element(A + "PayeeFinancialAccount");
        string Party(string type) {
            var party = root.Element(A + type)?.Element(A + "Party");
            return Text(party?.Element(A + "PartyName"), B + "Name") is { Length: > 0 } name
                ? name : Text(party?.Element(A + "PartyLegalEntity"), B + "RegistrationName");
        }
        var invoice = new Invoice {
            Number = Text(root, B + "ID"), FileName = Path.GetFileName(fileName), OriginalXml = bytes,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)), CustomizationId = custom,
            ProfileId = Text(root, B + "ProfileID"), IssueDate = Text(root, B + "IssueDate"), DueDate = Text(root, B + "DueDate"),
            Currency = Text(root, B + "DocumentCurrencyCode"), Supplier = Party("AccountingSupplierParty"), Buyer = Party("AccountingCustomerParty"),
            BuyerReference = Text(root, B + "BuyerReference"), OrderReference = Text(root.Element(A + "OrderReference"), B + "ID"),
            DespatchReference = Text(root.Element(A + "DespatchDocumentReference"), B + "ID"), Note = Text(root, B + "Note"),
            PaymentAccount = Text(account, B + "ID"), Net = Amount(totals, B + "TaxExclusiveAmount"),
            Tax = Amount(root.Element(A + "TaxTotal"), B + "TaxAmount"), Payable = Amount(totals, B + "PayableAmount")
        };
        if (string.IsNullOrWhiteSpace(invoice.Number)) throw new InvalidDataException("Invoice number is required.");
        foreach (var line in root.Elements(A + "InvoiceLine")) {
            var item = line.Element(A + "Item");
            var code = item?.Element(A + "StandardItemIdentification")?.Element(B + "ID");
            var tax = item?.Element(A + "ClassifiedTaxCategory");
            invoice.Lines.Add(new InvoiceLine {
                Number = Text(line, B + "ID"), Name = Text(item, B + "Name"), ItemCode = code?.Value ?? "", Scheme = code?.Attribute("schemeID")?.Value ?? "",
                Quantity = Amount(line, B + "InvoicedQuantity"), Unit = line.Element(B + "InvoicedQuantity")?.Attribute("unitCode")?.Value ?? "",
                Price = Amount(line.Element(A + "Price"), B + "PriceAmount"), Net = Amount(line, B + "LineExtensionAmount"),
                TaxCategory = Text(tax, B + "ID"), TaxRate = Amount(tax, B + "Percent")
            });
        }
        if (invoice.Lines.Count == 0) throw new InvalidDataException("At least one invoice line is required.");
        void Capture(XElement element, string path) {
            foreach (var attribute in element.Attributes().Where(x => !x.IsNamespaceDeclaration))
                invoice.Fields.Add(new() { Path = path + "/@" + attribute.Name, Value = attribute.Value });
            if (!element.HasElements) invoice.Fields.Add(new() { Path = path, Value = element.Value });
            var counts = new Dictionary<XName, int>();
            foreach (var child in element.Elements()) {
                counts.TryGetValue(child.Name, out var count); counts[child.Name] = ++count;
                Capture(child, path + "/" + child.Name.LocalName + "[" + count + "]");
            }
        }
        Capture(root, "Invoice");
        var checks = new List<string> { "Parsed successfully. Full XRechnung validation has not been performed." };
        if (invoice.Lines.Sum(x => x.Net) != invoice.Net) checks.Add("Warning: line total differs from invoice net amount.");
        if (invoice.Net + invoice.Tax != Amount(totals, B + "TaxInclusiveAmount")) checks.Add("Warning: net plus tax differs from gross amount.");
        if (invoice.PaymentAccount.StartsWith("IBAN", StringComparison.OrdinalIgnoreCase)) checks.Add("Account contains an IBAN prefix; original value retained.");
        invoice.Checks = string.Join("\n", checks);
        return invoice;
    }
}
