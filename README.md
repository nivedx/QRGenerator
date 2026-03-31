# QRGenerator

A .NET Windows Forms application for injecting QR codes into PDF files.

## Features

- **PDF Upload & Preview**: Select a PDF file and view its contents page by page within the application.
- **Page Navigation**: Navigate between pages using Previous/Next buttons.
- **Page Selection**: Select a specific page to target for QR code injection.
- **QR Code Injection**: Generate a dynamic QR code containing:
  - Bank Name: ADIB
  - Created Date: Current date/time
  - ID: A uniquely generated GUID
- The QR code is placed at the **bottom-right corner** of the selected page.
- **Save PDF**: Save the modified PDF with a prefilled filename (`{original_name}_{GUID}.pdf`), with the option to change the name and location.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows (or enable `EnableWindowsTargeting` for cross-platform builds)

## NuGet Dependencies

| Package | Purpose |
|---------|---------|
| [QRCoder](https://www.nuget.org/packages/QRCoder) | QR code generation |
| [PDFsharp](https://www.nuget.org/packages/PDFsharp) | PDF reading, modification, and saving |
| [Docnet.Core](https://www.nuget.org/packages/Docnet.Core) | PDF page rendering for preview |

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/nivedx/QRGenerator.git
   cd QRGenerator
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

## Usage

1. Click **Select PDF** to choose a PDF file from your system.
2. Browse pages using **< Prev** and **Next >** buttons.
3. Navigate to the desired page and click **Select This Page**.
4. Click **Inject QR Code** to generate and embed the QR code on the selected page.
5. Click **Save PDF** to save the modified file. The filename is prefilled as `{original_name}_{GUID}.pdf`.

## Project Structure

| File | Description |
|------|-------------|
| `QRGenerator.csproj` | Project file with NuGet dependencies |
| `Program.cs` | Application entry point |
| `Form1.cs` | Main form logic (PDF loading, QR generation, injection, saving) |
| `Form1.Designer.cs` | Designer-generated UI layout |
