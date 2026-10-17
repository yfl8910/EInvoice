namespace EInvoice.Web.Data;

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public byte[] OriginalXml { get; set; } = [];
    public DateTime ImportedUtc { get; set; } = DateTime.UtcNow;
    public string IssueDate { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string Currency { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string Buyer { get; set; } = "";
    public string BuyerReference { get; set; } = "";
    public string OrderReference { get; set; } = "";
    public string DespatchReference { get; set; } = "";
    public string CustomizationId { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public string Note { get; set; } = "";
    public string PaymentAccount { get; set; } = "";
    public decimal Net { get; set; }
    public decimal Tax { get; set; }
    public decimal Payable { get; set; }
    public string Checks { get; set; } = "";
    public List<InvoiceLine> Lines { get; set; } = [];
    public List<InvoiceField> Fields { get; set; } = [];
}

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string Scheme { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Net { get; set; }
    public string TaxCategory { get; set; } = "";
    public decimal TaxRate { get; set; }
}

// Retains every leaf value and attribute, including fields outside the summary model.
public class InvoiceField
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Path { get; set; } = "";
    public string Value { get; set; } = "";
}
