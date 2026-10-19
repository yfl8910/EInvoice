# UI components

Pages load data and coordinate actions. Components receive data through parameters and report changes through callbacks; they do not access the invoice database.

| Area | Components | Responsibility |
| --- | --- | --- |
| Layout | `AppSidebar` | Navigation and responsive menu styling |
| Shared | `PageHeader`, `InvoiceSearchBox`, `DismissibleMessage`, `MessageDialog` | Page titles/actions, compact search, feedback and accessible native dialogs |
| Import | `InvoiceUploadPanel`, `UploadedInvoiceList`, `InvoiceImportActions` | File selection, selected-file status and validation/save controls |
| Invoice display | `InvoiceListTable`, `InvoicePreview`, `InvoiceLinesTable`, `InvoiceFieldsTable`, `InvoiceDownloadActions` | Sortable/paged list, preview, line items, original fields and downloads |
| Extended invoice data | `InvoiceAdditionalDetails` | Addresses, tax IDs, delivery, banking, tax breakdown and original totals, projected from saved XML fields |
| Analysis | `InvoiceAnalysisFilters`, `CurrencySummaryCards`, `InvoiceAnalysisTable` | Controlled filters, separate currency totals and grouped results |

`InvoiceAnalysisFilter` holds immutable filter values and matching rules. `PendingInvoice` holds upload workflow state. Import validation and persistence remain in the import page and services.

Use explicit expressions for dynamic string parameters, for example `Search="@search"` and `Dimension="@filter.Dimension"`. Use `@bind-Value` or the matching value/callback parameters for controlled inputs.

`MessageDialog.ShowAsync(text)` owns its JavaScript module and lifetime. It returns false if the browser cannot show the dialog, allowing the page to display an inline fallback. The dialog retains its Close buttons, double-click and native Escape behavior.

Keep component styles beside their Razor file. The message dialog module is published from `Components/Shared/MessageDialog.razor.js`; page files no longer own that module. Extended upload and analysis guidance is displayed in expandable details to keep the primary UI concise.

`InvoiceDetailProjection` is shared by the UI and PDF export. It reads the existing `Invoice.Fields` collection without changing database columns or source files. Missing optional values remain absent, and PDF-only reference information is not inferred or inserted into the invoice. See `docs/invoice-6250035639-comparison.md` for the reference audit.
