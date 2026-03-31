using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using QRCoder;

namespace QRGenerator;

public partial class Form1 : Form
{
    private byte[]? _pdfBytes;
    private string? _originalFileName;
    private int _currentPageIndex;
    private int _totalPages;
    private int _selectedPageIndex = -1;
    private string? _generatedGuid;
    private bool _qrInjected;

    public Form1()
    {
        InitializeComponent();
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        btnSelectPage.Enabled = _pdfBytes != null;
        btnInjectQR.Enabled = _pdfBytes != null && _selectedPageIndex >= 0 && !_qrInjected;
        btnSavePDF.Enabled = _qrInjected;
        btnPrevPage.Enabled = _pdfBytes != null && _currentPageIndex > 0;
        btnNextPage.Enabled = _pdfBytes != null && _currentPageIndex < _totalPages - 1;
        lblPageInfo.Text = _pdfBytes != null
            ? $"Page {_currentPageIndex + 1} of {_totalPages}"
            : "No PDF loaded";
        lblSelectedPage.Text = _selectedPageIndex >= 0
            ? $"Selected: Page {_selectedPageIndex + 1}"
            : "No page selected";
    }

    private void BtnSelectPDF_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Select a PDF file"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _pdfBytes = File.ReadAllBytes(dialog.FileName);
            _originalFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
            _currentPageIndex = 0;
            _selectedPageIndex = -1;
            _qrInjected = false;
            _generatedGuid = null;

            using var docReader = DocLib.Instance.GetDocReader(_pdfBytes, new PageDimensions(1080, 1528));
            _totalPages = docReader.GetPageCount();

            lblFileName.Text = Path.GetFileName(dialog.FileName);
            RenderCurrentPage();
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading PDF: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderCurrentPage()
    {
        if (_pdfBytes == null) return;

        try
        {
            using var docReader = DocLib.Instance.GetDocReader(_pdfBytes, new PageDimensions(1080, 1528));
            using var pageReader = docReader.GetPageReader(_currentPageIndex);

            var rawBytes = pageReader.GetImage();
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0 || width <= 0 || height <= 0)
            {
                pictureBoxPreview.Image?.Dispose();
                pictureBoxPreview.Image = null;
                return;
            }

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            int srcStride = width * 4;
            if (bmpData.Stride == srcStride)
            {
                Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
            }
            else
            {
                for (int row = 0; row < height; row++)
                {
                    Marshal.Copy(rawBytes, row * srcStride,
                        bmpData.Scan0 + row * bmpData.Stride, srcStride);
                }
            }

            bmp.UnlockBits(bmpData);

            pictureBoxPreview.Image?.Dispose();
            pictureBoxPreview.Image = bmp;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error rendering page: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnPrevPage_Click(object? sender, EventArgs e)
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            RenderCurrentPage();
            UpdateButtonStates();
        }
    }

    private void BtnNextPage_Click(object? sender, EventArgs e)
    {
        if (_currentPageIndex < _totalPages - 1)
        {
            _currentPageIndex++;
            RenderCurrentPage();
            UpdateButtonStates();
        }
    }

    private void BtnSelectPage_Click(object? sender, EventArgs e)
    {
        _selectedPageIndex = _currentPageIndex;
        UpdateButtonStates();
    }

    private void BtnInjectQR_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null || _selectedPageIndex < 0) return;

        string? tempQrPath = null;
        try
        {
            _generatedGuid = Guid.NewGuid().ToString();
            var createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var qrData = $"Bank Name: ADIB\nCreated Date: {createdDate}\nID: {_generatedGuid}";

            // Generate QR code image
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrBitmap = qrCode.GetGraphic(20);

            // Save QR code to a temporary PNG file for PDFsharp to load
            tempQrPath = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid()}.png");
            qrBitmap.Save(tempQrPath, ImageFormat.Png);

            // Open PDF with PDFsharp and inject QR code
            using var pdfStream = new MemoryStream(_pdfBytes);
            var document = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Modify);
            var page = document.Pages[_selectedPageIndex];

            using (var gfx = XGraphics.FromPdfPage(page))
            using (var xImage = XImage.FromFile(tempQrPath))
            {
                double qrSize = 72;
                double margin = 20;
                double x = page.Width.Point - qrSize - margin;
                double y = page.Height.Point - qrSize - margin;

                gfx.DrawImage(xImage, x, y, qrSize, qrSize);
            }

            // Save modified PDF to byte array
            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            _pdfBytes = outputStream.ToArray();

            _qrInjected = true;

            // Show the injected page in preview
            _currentPageIndex = _selectedPageIndex;
            RenderCurrentPage();
            UpdateButtonStates();

            MessageBox.Show(
                $"QR Code injected on page {_selectedPageIndex + 1}.\n\nEmbedded Data:\n{qrData}",
                "QR Code Injected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error injecting QR code: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (tempQrPath != null && File.Exists(tempQrPath))
            {
                try { File.Delete(tempQrPath); }
                catch { /* ignore cleanup errors */ }
            }
        }
    }

    private void BtnSavePDF_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null || !_qrInjected) return;

        var defaultFileName = $"{_originalFileName}_{_generatedGuid}.pdf";

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title = "Save PDF",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _pdfBytes);
            MessageBox.Show("PDF saved successfully!", "Save Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving PDF: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
