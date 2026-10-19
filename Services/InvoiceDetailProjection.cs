using System.Text.RegularExpressions;
using EInvoice.Web.Data;

namespace EInvoice.Web.Services;

public record InvoiceDetailEntry(string Label, string Value, string XmlPath);
public record InvoiceDetailSection(string Title, IReadOnlyList<InvoiceDetailEntry> Entries);

// Uses the already persisted XML leaf fields, so existing invoices need no schema migration or re-import.
public static class InvoiceDetailProjection
{
    public static IReadOnlyList<InvoiceDetailSection> Build(Invoice invoice)
    {
        var fields = invoice.Fields.GroupBy(x => x.Path).ToDictionary(x => x.Key, x => x.First().Value);
        var sections = new List<InvoiceDetailSection>();
        string Value(string path) => fields.GetValueOrDefault(path, "");
        IEnumerable<string> Children(string parent, string name) => fields.Keys
            .Select(x => Regex.Match(x, "^" + Regex.Escape(parent + "/" + name) + @"\[\d+\]"))
            .Where(x => x.Success).Select(x => x.Value).Distinct();
        void Section(string title, Action<List<InvoiceDetailEntry>> fill) {
            var entries = new List<InvoiceDetailEntry>(); fill(entries);
            if (entries.Count > 0) sections.Add(new(title, entries));
        }
        void Add(List<InvoiceDetailEntry> entries, string label, string path) {
            if (!string.IsNullOrWhiteSpace(Value(path))) entries.Add(new(label, Value(path), path));
        }
        void Address(List<InvoiceDetailEntry> entries, string label, string prefix) {
            var paths = new[] { "StreetName[1]", "AdditionalStreetName[1]", "BuildingNumber[1]", "PostalZone[1]", "CityName[1]", "CountrySubentity[1]", "Country[1]/IdentificationCode[1]" }
                .Select(x => prefix + "/" + x).Concat(Children(prefix, "AddressLine").Select(x => x + "/Line[1]"));
            var present = paths.Where(x => !string.IsNullOrWhiteSpace(Value(x))).ToList();
            if (present.Count > 0) entries.Add(new(label, string.Join(", ", present.Select(Value)), string.Join("; ", present)));
        }
        void Party(string title, string prefix) => Section(title, entries => {
            Address(entries, "Address", prefix + "/PostalAddress[1]");
            foreach (var tax in Children(prefix, "PartyTaxScheme")) {
                var scheme = Value(tax + "/TaxScheme[1]/ID[1]");
                Add(entries, scheme.Length > 0 ? $"Tax ID ({scheme})" : "Tax ID", tax + "/CompanyID[1]");
            }
            Add(entries, "Electronic address", prefix + "/EndpointID[1]");
            Add(entries, "Electronic address scheme", prefix + "/EndpointID[1]/@schemeID");
            foreach (var legal in Children(prefix, "PartyLegalEntity")) {
                Add(entries, "Legal name", legal + "/RegistrationName[1]");
                Add(entries, "Legal entity ID (original)", legal + "/CompanyID[1]");
            }
            foreach (var contact in Children(prefix, "Contact")) {
                Add(entries, "Contact", contact + "/Name[1]");
                Add(entries, "Telephone", contact + "/Telephone[1]");
                Add(entries, "Email", contact + "/ElectronicMail[1]");
            }
        });
        Section("Document details", entries => {
            Add(entries, "Invoice type code", "Invoice/InvoiceTypeCode[1]");
            foreach (var note in Children("Invoice", "Note")) Add(entries, "Note / payment terms (original)", note);
            foreach (var terms in Children("Invoice", "PaymentTerms")) Add(entries, "Payment terms", terms + "/Note[1]");
        });
        Party("Supplier details", "Invoice/AccountingSupplierParty[1]/Party[1]");
        Party("Buyer details", "Invoice/AccountingCustomerParty[1]/Party[1]");
        foreach (var delivery in Children("Invoice", "Delivery")) Section("Delivery", entries => {
            Add(entries, "Actual delivery date (XML)", delivery + "/ActualDeliveryDate[1]");
            Add(entries, "Recipient", delivery + "/DeliveryParty[1]/PartyName[1]/Name[1]");
            Add(entries, "Delivery location ID", delivery + "/DeliveryLocation[1]/ID[1]");
            Address(entries, "Delivery address", delivery + "/DeliveryLocation[1]/Address[1]");
        });
        foreach (var payment in Children("Invoice", "PaymentMeans")) Section("Payment details", entries => {
            Add(entries, "Payment means code", payment + "/PaymentMeansCode[1]");
            Add(entries, "Payment reference", payment + "/PaymentID[1]");
            Add(entries, "Account (original)", payment + "/PayeeFinancialAccount[1]/ID[1]");
            Add(entries, "Account name", payment + "/PayeeFinancialAccount[1]/Name[1]");
            Add(entries, "Bank / branch name (original)", payment + "/PayeeFinancialAccount[1]/FinancialInstitutionBranch[1]/Name[1]");
            Add(entries, "Bank / branch ID (original)", payment + "/PayeeFinancialAccount[1]/FinancialInstitutionBranch[1]/ID[1]");
        });
        foreach (var taxTotal in Children("Invoice", "TaxTotal"))
            foreach (var tax in Children(taxTotal, "TaxSubtotal")) Section("Tax breakdown", entries => {
                Add(entries, "Tax category", tax + "/TaxCategory[1]/ID[1]");
                Add(entries, "Tax rate (%)", tax + "/TaxCategory[1]/Percent[1]");
                Add(entries, "Taxable amount", tax + "/TaxableAmount[1]");
                Add(entries, "Tax amount", tax + "/TaxAmount[1]");
                Add(entries, "Currency", tax + "/TaxAmount[1]/@currencyID");
                Add(entries, "Tax exemption reason", tax + "/TaxCategory[1]/TaxExemptionReason[1]");
            });
        Section("Invoice totals (original)", entries => {
            var prefix = "Invoice/LegalMonetaryTotal[1]/";
            foreach (var (label, name) in new[] {
                ("Line total", "LineExtensionAmount"), ("Net", "TaxExclusiveAmount"), ("Gross (including tax)", "TaxInclusiveAmount"),
                ("Allowances", "AllowanceTotalAmount"), ("Charges", "ChargeTotalAmount"), ("Prepaid", "PrepaidAmount"),
                ("Rounding", "PayableRoundingAmount"), ("Payable", "PayableAmount") }) Add(entries, label, prefix + name + "[1]");
        });
        return sections;
    }
}
