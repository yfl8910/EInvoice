namespace EInvoice.Web.Data;

public sealed class PendingInvoice
{
    public string Name { get; init; } = "";
    public Invoice? Invoice { get; set; }
    public string Error { get; set; } = "";
    public string Validation { get; set; } = "";
    public bool Validated { get; set; }
    public int? SavedId { get; set; }
}
