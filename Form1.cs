using System.Drawing.Drawing2D;
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
    private const int BytesPerPixel = 4; // BGRA / ARGB format = 4 bytes per pixel
    private const int MinSelectionSize = 5; // Minimum drag size in image pixels to register as a selection

    private byte[]? _pdfBytes;
    private string? _originalFileName;
    private int _currentPageIndex;
    private int _totalPages;
    private int _selectedPageIndex = -1;
    private string? _generatedGuid;
    private bool _qrInjected;

    // Area-selection state for QR placement
    private bool _isSelectingArea;
    private bool _isDragging;
    private Point _dragStart;
    private Point _dragCurrent;
    private RectangleF _selectedAreaInImage;
    private Size _selectedAreaImageSize;
    private bool _isAreaSelected;

    public Form1()
    {
        InitializeComponent();
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        btnSelectPage.Enabled = _pdfBytes != null;
        btnInjectQR.Enabled = _pdfBytes != null && _selectedPageIndex >= 0 && _isAreaSelected && !_qrInjected;
        btnSavePDF.Enabled = _qrInjected;
        btnPrevPage.Enabled = _pdfBytes != null && _currentPageIndex > 0;
        btnNextPage.Enabled = _pdfBytes != null && _currentPageIndex < _totalPages - 1;
        lblPageInfo.Text = _pdfBytes != null
            ? $"Page {_currentPageIndex + 1} of {_totalPages}"
            : "No PDF loaded";
        lblSelectedPage.Text = GetSelectedPageStatus();
    }

    private string GetSelectedPageStatus()
    {
        if (_selectedPageIndex < 0)
            return "No page selected";
        if (_isAreaSelected)
            return $"Page {_selectedPageIndex + 1} \u2013 Area set";
        if (_isSelectingArea)
            return $"Page {_selectedPageIndex + 1} \u2013 Draw QR area";
        return $"Selected: Page {_selectedPageIndex + 1}";
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
            _isSelectingArea = false;
            _isDragging = false;
            _isAreaSelected = false;
            pictureBoxPreview.Cursor = Cursors.Default;

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

            int srcStride = width * BytesPerPixel;
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
        _isAreaSelected = false;
        _isSelectingArea = true;
        pictureBoxPreview.Cursor = Cursors.Cross;
        UpdateButtonStates();
        pictureBoxPreview.Invalidate();
    }

    private void PictureBoxPreview_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_isSelectingArea || e.Button != MouseButtons.Left ||
            _currentPageIndex != _selectedPageIndex || pictureBoxPreview.Image == null)
            return;

        _isDragging = true;
        _dragStart = e.Location;
        _dragCurrent = e.Location;
        _isAreaSelected = false;
        UpdateButtonStates();
        pictureBoxPreview.Invalidate();
    }

    private void PictureBoxPreview_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        _dragCurrent = e.Location;
        pictureBoxPreview.Invalidate();
    }

    private void PictureBoxPreview_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || e.Button != MouseButtons.Left) return;

        _isDragging = false;
        _dragCurrent = e.Location;

        var startImg = ControlToImageCoords(_dragStart);
        var endImg = ControlToImageCoords(_dragCurrent);

        float x = Math.Min(startImg.X, endImg.X);
        float y = Math.Min(startImg.Y, endImg.Y);
        float w = Math.Abs(endImg.X - startImg.X);
        float h = Math.Abs(endImg.Y - startImg.Y);

        if (w > MinSelectionSize && h > MinSelectionSize)
        {
            _selectedAreaInImage = new RectangleF(x, y, w, h);
            _selectedAreaImageSize = pictureBoxPreview.Image!.Size;
            _isAreaSelected = true;
            _isSelectingArea = false;
            pictureBoxPreview.Cursor = Cursors.Default;
        }

        UpdateButtonStates();
        pictureBoxPreview.Invalidate();
    }

    private void PictureBoxPreview_Paint(object? sender, PaintEventArgs e)
    {
        if (_isDragging)
        {
            var rect = GetDragRectangle(_dragStart, _dragCurrent);
            using var pen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, rect);
        }
        else if (_isAreaSelected && _currentPageIndex == _selectedPageIndex &&
                 pictureBoxPreview.Image != null)
        {
            var displayRect = GetImageDisplayRect();
            if (displayRect.IsEmpty) return;

            float scaleX = (float)displayRect.Width / pictureBoxPreview.Image.Width;
            float scaleY = (float)displayRect.Height / pictureBoxPreview.Image.Height;

            var rect = new Rectangle(
                (int)(displayRect.X + _selectedAreaInImage.X * scaleX),
                (int)(displayRect.Y + _selectedAreaInImage.Y * scaleY),
                (int)(_selectedAreaInImage.Width * scaleX),
                (int)(_selectedAreaInImage.Height * scaleY));

            using var pen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            using var brush = new SolidBrush(Color.FromArgb(40, Color.Red));
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    private Rectangle GetImageDisplayRect()
    {
        if (pictureBoxPreview.Image == null) return Rectangle.Empty;

        var imgSize = pictureBoxPreview.Image.Size;
        var ctrlSize = pictureBoxPreview.ClientSize;

        float ratioX = (float)ctrlSize.Width / imgSize.Width;
        float ratioY = (float)ctrlSize.Height / imgSize.Height;
        float ratio = Math.Min(ratioX, ratioY);

        int displayW = (int)(imgSize.Width * ratio);
        int displayH = (int)(imgSize.Height * ratio);
        int displayX = (ctrlSize.Width - displayW) / 2;
        int displayY = (ctrlSize.Height - displayH) / 2;

        return new Rectangle(displayX, displayY, displayW, displayH);
    }

    private PointF ControlToImageCoords(Point controlPt)
    {
        var displayRect = GetImageDisplayRect();
        if (displayRect.IsEmpty || pictureBoxPreview.Image == null)
            return PointF.Empty;

        float imgX = (controlPt.X - displayRect.X) * pictureBoxPreview.Image.Width / (float)displayRect.Width;
        float imgY = (controlPt.Y - displayRect.Y) * pictureBoxPreview.Image.Height / (float)displayRect.Height;

        imgX = Math.Clamp(imgX, 0, pictureBoxPreview.Image.Width);
        imgY = Math.Clamp(imgY, 0, pictureBoxPreview.Image.Height);

        return new PointF(imgX, imgY);
    }

    private static Rectangle GetDragRectangle(Point start, Point end)
    {
        return new Rectangle(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));
    }

    private void BtnInjectQR_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null || _selectedPageIndex < 0 || !_isAreaSelected) return;

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
                // Convert selected area from image coordinates to PDF coordinates
                double scaleX = page.Width.Point / _selectedAreaImageSize.Width;
                double scaleY = page.Height.Point / _selectedAreaImageSize.Height;

                double pdfX = _selectedAreaInImage.X * scaleX;
                double pdfY = _selectedAreaInImage.Y * scaleY;
                double pdfW = _selectedAreaInImage.Width * scaleX;
                double pdfH = _selectedAreaInImage.Height * scaleY;

                gfx.DrawImage(xImage, pdfX, pdfY, pdfW, pdfH);
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
                catch { /* Cleanup of temp file is best-effort; failure is non-critical */ }
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
