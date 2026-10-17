# EInvoice

Blazor invoice management application targeting .NET 11, with EF Core 11 and SQLite.

## Requirements

- .NET SDK `11.0.100-rc.2.26504.105` (the latest .NET 11 release as of October 17, 2026).
- .NET 11 is currently a release candidate, not a final release.
- [Official SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/11.0).

`global.json` selects the RC2 SDK, checking `.dotnet`, then the parent directory's `.dotnet`, and finally the system installation. This workspace uses the verified Windows x64 SDK in `../.dotnet`. SDK binaries, restored packages, databases and generated files are excluded from version control. NuGet packages are restored into `.packages`.

## Run

From the repository root:

```powershell
dotnet --version
dotnet restore EInvoice.Web.csproj
dotnet run --project EInvoice.Web.csproj --no-launch-profile --urls http://127.0.0.1:5080
```

Open http://127.0.0.1:5080. The three navigation entries are Invoice Upload, Invoice List and Invoice Analysis. Uploading and validating do not save an invoice until Save is clicked.

When launching a compiled DLL directly with the local runtime, use `..\.dotnet\dotnet.exe` instead of the system host, unless RC2 is also installed system-wide.

Debug and Release executables built in this repository are configured to locate `.dotnet` relative to their output directory, falling back to the system installation. This allows direct execution and IDE debugging without a system-wide RC2 installation. Rebuild after moving the build output or installing the local SDK. Published applications are self-contained and include the required runtime; a system-wide RC2 installation is not needed.

## Publish and run

```powershell
dotnet publish EInvoice.Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
./publish/EInvoice.Web.exe --contentRoot ./publish --urls http://127.0.0.1:5080
```

Deploy the entire publish directory, including `wwwroot` and `EInvoice.Web.staticwebassets.endpoints.json`. Start the executable in that directory, or set `--contentRoot` to its absolute path when starting it from another directory. Keep the published endpoint manifest together with the matching published files; do not replace it with the build manifest from `bin/Release/net11.0`.

The FolderProfile publish profile also uses self-contained deployment. Run `EInvoice.Web.exe` with the complete publish directory in place. It starts a web server; open the address shown in its console in a browser. Existing files and `App_Data` are preserved during folder publishing.

The default runtime identifier is `win-x64`. New builds and publishes select only Windows x64 native dependencies, including SQLite, instead of copying native libraries for Android, iOS, Linux and other platforms. Default build output is under `bin/<Configuration>/<TargetFramework>/win-x64`; the folder publish profile inherits the project's target framework. Older output directories from portable builds are not used or automatically removed.

Build output is also supported outside Development: startup explicitly enables its static web asset manifest, allowing scoped CSS, Blazor scripts and component JavaScript modules to load in Production or Staging. Published output serves its packaged assets from `wwwroot`.

## Verification

```powershell
dotnet build EInvoice.Web.csproj -c Release
dotnet run --project EInvoice.Web.csproj -- --verify-pdf
```

PDF verification generates six sample PDFs and a 120-line multipage PDF under `tmp/pdfs`, without writing invoices to the database. Windows requires Arial; Linux requires DejaVu Sans under `/usr/share/fonts/truetype/dejavu`.

`--verify-samples` checks XML parsing, preview without saving, persisted invoices and lines, duplicates, currency-aware analysis, original bytes, XML attributes, malformed XML and DTD rejection. It imports sample invoices into the selected content root's database; use a separate test content root and a sibling `电子发票` directory when testing without changing application data.

The application database remains at `EInvoice.Web/App_Data/invoices.db`. This framework upgrade does not change the invoice schema. Compatibility was verified against a SQLite backup of the existing database, in addition to a fresh test database.

