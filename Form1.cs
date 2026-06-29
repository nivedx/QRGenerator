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
    private const int BytesPerPixel    = 4;
    private const int MinSelectionSize = 5;

    private byte[]? _pdfBytes;
    private string? _originalFileName;
    private int _currentPageIndex;
    private int _totalPages;
    private int _selectedPageIndex = -1;
    private string? _generatedGuid;
    private bool _qrInjected;
    private bool _isUploading;
    private OneDriveSettings _settings = new();

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
        _settings = OneDriveSettings.Load();
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        btnSelectPDF.Enabled   = !_isUploading;
        btnReset.Enabled       = !_isUploading && _pdfBytes != null;
        btnSettings.Enabled    = !_isUploading;
        btnSelectPage.Enabled  = !_isUploading && _pdfBytes != null;
        btnInjectQR.Enabled    = !_isUploading && _pdfBytes != null && _selectedPageIndex >= 0 && _isAreaSelected && !_qrInjected;
        btnQuickInject.Enabled = !_isUploading && _pdfBytes != null && !_qrInjected;
        btnSavePDF.Enabled     = !_isUploading && _qrInjected;
        btnPrevPage.Enabled    = !_isUploading && _pdfBytes != null && _currentPageIndex > 0;
        btnNextPage.Enabled    = !_isUploading && _pdfBytes != null && _currentPageIndex < _totalPages - 1;
        UseWaitCursor          = _isUploading;
        lblPageInfo.Text = _pdfBytes != null
            ? $"Page {_currentPageIndex + 1} of {_totalPages}"
            : "No PDF loaded";
        lblSelectedPage.Text = GetSelectedPageStatus();
    }

    private string GetSelectedPageStatus()
    {
        if (_isUploading)
            return "Uploading to OneDrive…";
        if (_selectedPageIndex < 0)
            return "No page selected";
        if (_isAreaSelected)
            return $"Page {_selectedPageIndex + 1} – Area set";
        if (_isSelectingArea)
            return $"Page {_selectedPageIndex + 1} – Draw QR area";
        return $"Selected: Page {_selectedPageIndex + 1}";
    }

    // ── Reset ──────────────────────────────────────────────────────────────

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        if (_qrInjected)
        {
            var confirm = MessageBox.Show(
                "The current document has an injected QR code that has not been saved.\n\nReset anyway?",
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
        }

        pictureBoxPreview.Image?.Dispose();
        pictureBoxPreview.Image  = null;
        pictureBoxPreview.Cursor = Cursors.Default;

        _pdfBytes          = null;
        _originalFileName  = null;
        _currentPageIndex  = 0;
        _totalPages        = 0;
        _selectedPageIndex = -1;
        _generatedGuid     = null;
        _qrInjected        = false;
        _isSelectingArea   = false;
        _isDragging        = false;
        _isAreaSelected    = false;

        lblFileName.Text = "No file selected";
        UpdateButtonStates();
        pictureBoxPreview.Invalidate();
    }

    // ── PDF loading ────────────────────────────────────────────────────────

    private void BtnSelectPDF_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Title  = "Select a PDF file"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            _pdfBytes          = File.ReadAllBytes(dialog.FileName);
            _originalFileName  = Path.GetFileNameWithoutExtension(dialog.FileName);
            _currentPageIndex  = 0;
            _selectedPageIndex = -1;
            _qrInjected        = false;
            _generatedGuid     = null;
            _isSelectingArea   = false;
            _isDragging        = false;
            _isAreaSelected    = false;
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
            using var docReader  = DocLib.Instance.GetDocReader(_pdfBytes, new PageDimensions(1080, 1528));
            using var pageReader = docReader.GetPageReader(_currentPageIndex);

            var rawBytes = pageReader.GetImage();
            var width    = pageReader.GetPageWidth();
            var height   = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0 || width <= 0 || height <= 0)
            {
                pictureBoxPreview.Image?.Dispose();
                pictureBoxPreview.Image = null;
                return;
            }

            var bmp     = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int srcStride = width * BytesPerPixel;
            if (bmpData.Stride == srcStride)
            {
                Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
            }
            else
            {
                for (int row = 0; row < height; row++)
                    Marshal.Copy(rawBytes, row * srcStride,
                        bmpData.Scan0 + row * bmpData.Stride, srcStride);
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

    // ── Page navigation ────────────────────────────────────────────────────

    private void BtnPrevPage_Click(object? sender, EventArgs e)
    {
        if (_currentPageIndex <= 0) return;
        _currentPageIndex--;
        RenderCurrentPage();
        UpdateButtonStates();
    }

    private void BtnNextPage_Click(object? sender, EventArgs e)
    {
        if (_currentPageIndex >= _totalPages - 1) return;
        _currentPageIndex++;
        RenderCurrentPage();
        UpdateButtonStates();
    }

    // ── Area selection ─────────────────────────────────────────────────────

    private void BtnSelectPage_Click(object? sender, EventArgs e)
    {
        _selectedPageIndex = _currentPageIndex;
        _isAreaSelected    = false;
        _isSelectingArea   = true;
        pictureBoxPreview.Cursor = Cursors.Cross;
        UpdateButtonStates();
        pictureBoxPreview.Invalidate();
    }

    private void PictureBoxPreview_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_isSelectingArea || e.Button != MouseButtons.Left ||
            _currentPageIndex != _selectedPageIndex || pictureBoxPreview.Image == null)
            return;

        _isDragging   = true;
        _dragStart    = e.Location;
        _dragCurrent  = e.Location;
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

        _isDragging  = false;
        _dragCurrent = e.Location;

        var startImg = ControlToImageCoords(_dragStart);
        var endImg   = ControlToImageCoords(_dragCurrent);

        float x = Math.Min(startImg.X, endImg.X);
        float y = Math.Min(startImg.Y, endImg.Y);
        float w = Math.Abs(endImg.X - startImg.X);
        float h = Math.Abs(endImg.Y - startImg.Y);

        if (w > MinSelectionSize && h > MinSelectionSize)
        {
            _selectedAreaInImage  = new RectangleF(x, y, w, h);
            _selectedAreaImageSize = pictureBoxPreview.Image!.Size;
            _isAreaSelected       = true;
            _isSelectingArea      = false;
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

            float scaleX = (float)displayRect.Width  / pictureBoxPreview.Image.Width;
            float scaleY = (float)displayRect.Height / pictureBoxPreview.Image.Height;

            var rect = new Rectangle(
                (int)(displayRect.X + _selectedAreaInImage.X * scaleX),
                (int)(displayRect.Y + _selectedAreaInImage.Y * scaleY),
                (int)(_selectedAreaInImage.Width  * scaleX),
                (int)(_selectedAreaInImage.Height * scaleY));

            using var pen   = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash };
            using var brush = new SolidBrush(Color.FromArgb(40, Color.Red));
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    private Rectangle GetImageDisplayRect()
    {
        if (pictureBoxPreview.Image == null) return Rectangle.Empty;

        var imgSize  = pictureBoxPreview.Image.Size;
        var ctrlSize = pictureBoxPreview.ClientSize;

        float ratioX = (float)ctrlSize.Width  / imgSize.Width;
        float ratioY = (float)ctrlSize.Height / imgSize.Height;
        float ratio  = Math.Min(ratioX, ratioY);

        int displayW = (int)(imgSize.Width  * ratio);
        int displayH = (int)(imgSize.Height * ratio);
        int displayX = (ctrlSize.Width  - displayW) / 2;
        int displayY = (ctrlSize.Height - displayH) / 2;

        return new Rectangle(displayX, displayY, displayW, displayH);
    }

    private PointF ControlToImageCoords(Point controlPt)
    {
        var displayRect = GetImageDisplayRect();
        if (displayRect.IsEmpty || pictureBoxPreview.Image == null) return PointF.Empty;

        float imgX = (controlPt.X - displayRect.X) * pictureBoxPreview.Image.Width  / (float)displayRect.Width;
        float imgY = (controlPt.Y - displayRect.Y) * pictureBoxPreview.Image.Height / (float)displayRect.Height;

        return new PointF(
            Math.Clamp(imgX, 0, pictureBoxPreview.Image.Width),
            Math.Clamp(imgY, 0, pictureBoxPreview.Image.Height));
    }

    private static Rectangle GetDragRectangle(Point start, Point end) =>
        new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));

    // ── OneDrive settings ──────────────────────────────────────────────────

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
            _settings = form.Result;
    }

    private bool ValidateOneDriveSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.TenantId)     ||
            string.IsNullOrWhiteSpace(_settings.ClientId)     ||
            string.IsNullOrWhiteSpace(_settings.ClientSecret) ||
            string.IsNullOrWhiteSpace(_settings.UserEmail))
        {
            MessageBox.Show(
                "OneDrive settings are not configured.\n" +
                "Click '⚙ OneDrive Settings' to enter your Azure credentials.",
                "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    // ── QR injection ───────────────────────────────────────────────────────

    private async void BtnInjectQR_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null || _selectedPageIndex < 0 || !_isAreaSelected) return;
        if (!ValidateOneDriveSettings()) return;
        await PerformInjectionAsync(isQuickInject: false);
    }

    private async void BtnQuickInject_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null) return;
        if (!ValidateOneDriveSettings()) return;
        await PerformInjectionAsync(isQuickInject: true);
    }

    private async Task PerformInjectionAsync(bool isQuickInject)
    {
        string? tempQrPath = null;
        _isUploading = true;
        UpdateButtonStates();

        try
        {
            _generatedGuid = Guid.NewGuid().ToString();
            var fileName = $"{_generatedGuid}.pdf";

            using var uploader = new OneDriveUploader();
            var token = await uploader.AcquireTokenAsync(
                _settings.TenantId, _settings.ClientId, _settings.ClientSecret);

            // Step 1: Upload original PDF to claim the GUID filename on OneDrive.
            var itemId = await uploader.UploadPdfAsync(
                token, _settings.UserEmail, _settings.TargetFolder, fileName, _pdfBytes!);

            // Step 2: Create a public shareable link — this is the URL that goes in the QR.
            var shareableUrl = await uploader.CreateShareLinkAsync(
                token, _settings.UserEmail, itemId);

            // Step 3: Generate QR code containing the real, publicly accessible URL.
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData  = qrGenerator.CreateQrCode(shareableUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode      = new QRCode(qrCodeData);
            using var qrBitmap    = qrCode.GetGraphic(20);
            tempQrPath = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid()}.png");
            qrBitmap.Save(tempQrPath, ImageFormat.Png);

            // Step 4: Inject QR into a local copy of the PDF.
            _pdfBytes = InjectQrIntoPdf(tempQrPath, isQuickInject, out int targetPageIndex);

            _qrInjected        = true;
            _selectedPageIndex = targetPageIndex;
            _currentPageIndex  = targetPageIndex;
            RenderCurrentPage();

            // Step 5: Re-upload the QR-stamped PDF by item ID, overwriting the placeholder.
            // Using the item-ID endpoint avoids the 412 ETag conflict that the path-based
            // endpoint returns after createLink modifies the item's metadata in step 2.
            try
            {
                await uploader.ReplaceContentAsync(
                    token, _settings.UserEmail, itemId, _pdfBytes);

                MessageBox.Show(
                    $"QR stamped and document published to OneDrive.\n\n" +
                    $"Verification URL (embedded in QR):\n{shareableUrl}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // QR is injected locally; only the re-upload failed.
                MessageBox.Show(
                    $"QR injected, but re-upload of the stamped PDF failed:\n\n{ex.Message}\n\n" +
                    $"OneDrive currently holds the un-stamped version.\n" +
                    $"Use 'Save PDF' to save locally, then upload it manually.",
                    "Re-upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            if (!_qrInjected) _generatedGuid = null;
            MessageBox.Show($"Error: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isUploading = false;
            UpdateButtonStates();
            if (tempQrPath != null && File.Exists(tempQrPath))
                try { File.Delete(tempQrPath); } catch { }
        }
    }

    // Synchronous helper — opens the PDF, draws the QR, returns the new byte array.
    // All PDF/GDI resources are disposed before this method returns so that the
    // subsequent async upload does not hold on to large MemoryStream objects.
    private byte[] InjectQrIntoPdf(string tempQrPath, bool isQuickInject, out int targetPageIndex)
    {
        using var pdfStream = new MemoryStream(_pdfBytes!);
        using var document  = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Modify);

        if (isQuickInject)
        {
            targetPageIndex = document.PageCount - 1;
            var page = document.Pages[targetPageIndex];
            using var gfx  = XGraphics.FromPdfPage(page);
            using var xImg = XImage.FromFile(tempQrPath);
            const double qrSize = 80, margin = 20;

            // Use visual (displayed) dimensions so the QR lands at the visible
            // bottom-right regardless of whether the page has a /Rotate entry.
            double visW = VisualWidth(page);
            double visH = VisualHeight(page);
            var (rx, ry, rw, rh) = ToRawRect(page,
                visW - qrSize - margin, visH - qrSize - margin, qrSize, qrSize);
            gfx.DrawImage(xImg, rx, ry, rw, rh);
        }
        else
        {
            targetPageIndex = _selectedPageIndex;
            var page = document.Pages[targetPageIndex];
            using var gfx  = XGraphics.FromPdfPage(page);
            using var xImg = XImage.FromFile(tempQrPath);

            // Scale the user's image-pixel selection to visual PDF points, then
            // rotate those coordinates into the raw PDFsharp coordinate space.
            double scaleX = VisualWidth(page)  / _selectedAreaImageSize.Width;
            double scaleY = VisualHeight(page) / _selectedAreaImageSize.Height;
            double dx = _selectedAreaInImage.X     * scaleX;
            double dy = _selectedAreaInImage.Y     * scaleY;
            double dw = _selectedAreaInImage.Width * scaleX;
            double dh = _selectedAreaInImage.Height * scaleY;

            var (rx, ry, rw, rh) = ToRawRect(page, dx, dy, dw, dh);
            gfx.DrawImage(xImg, rx, ry, rw, rh);
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    // Visual (displayed) page width in points — swapped when /Rotate is 90 or 270.
    private static double VisualWidth(PdfSharp.Pdf.PdfPage page) =>
        page.Rotate is 90 or 270 ? page.Height.Point : page.Width.Point;

    // Visual (displayed) page height in points — swapped when /Rotate is 90 or 270.
    private static double VisualHeight(PdfSharp.Pdf.PdfPage page) =>
        page.Rotate is 90 or 270 ? page.Width.Point : page.Height.Point;

    // Transforms a rectangle expressed in visual/displayed PDF-point coordinates
    // (top-left origin, Y downward) into the raw PDFsharp drawing coordinate space,
    // accounting for the page's /Rotate entry (clockwise degrees).
    //
    // Derivation: for each rotation angle, corners are mapped as follows —
    //   Rotate 90  CW: raw(x,y) = (dy,      H−dx−dw),  raw w/h swapped
    //   Rotate 180 CW: raw(x,y) = (W−dx−dw, H−dy−dh),  same size
    //   Rotate 270 CW: raw(x,y) = (W−dy−dh, dx),        raw w/h swapped
    private static (double x, double y, double w, double h) ToRawRect(
        PdfSharp.Pdf.PdfPage page, double dx, double dy, double dw, double dh)
    {
        double W = page.Width.Point;
        double H = page.Height.Point;
        return page.Rotate switch
        {
             90 => (dy,          H - dx - dw, dh, dw),
            180 => (W - dx - dw, H - dy - dh, dw, dh),
            270 => (W - dy - dh, dx,           dh, dw),
            _   => (dx,          dy,           dw, dh),
        };
    }

    // ── Save ───────────────────────────────────────────────────────────────

    private void BtnSavePDF_Click(object? sender, EventArgs e)
    {
        if (_pdfBytes == null || !_qrInjected) return;

        using var dialog = new SaveFileDialog
        {
            Filter   = "PDF files (*.pdf)|*.pdf",
            Title    = "Save PDF",
            FileName = $"{_generatedGuid}.pdf"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

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
