namespace QRGenerator;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        btnSelectPDF = new Button();
        lblFileName = new Label();
        panelPreview = new Panel();
        pictureBoxPreview = new PictureBox();
        btnPrevPage = new Button();
        btnNextPage = new Button();
        lblPageInfo = new Label();
        btnSelectPage = new Button();
        lblSelectedPage = new Label();
        btnInjectQR = new Button();
        btnSavePDF = new Button();
        panelPreview.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
        SuspendLayout();

        // btnSelectPDF
        btnSelectPDF.Location = new Point(12, 12);
        btnSelectPDF.Name = "btnSelectPDF";
        btnSelectPDF.Size = new Size(120, 35);
        btnSelectPDF.TabIndex = 0;
        btnSelectPDF.Text = "Select PDF";
        btnSelectPDF.UseVisualStyleBackColor = true;
        btnSelectPDF.Click += BtnSelectPDF_Click;

        // lblFileName
        lblFileName.Location = new Point(140, 12);
        lblFileName.Name = "lblFileName";
        lblFileName.Size = new Size(640, 35);
        lblFileName.TabIndex = 1;
        lblFileName.Text = "No file selected";
        lblFileName.TextAlign = ContentAlignment.MiddleLeft;

        // panelPreview
        panelPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelPreview.AutoScroll = true;
        panelPreview.BorderStyle = BorderStyle.FixedSingle;
        panelPreview.Controls.Add(pictureBoxPreview);
        panelPreview.Location = new Point(12, 55);
        panelPreview.Name = "panelPreview";
        panelPreview.Size = new Size(960, 540);
        panelPreview.TabIndex = 2;

        // pictureBoxPreview
        pictureBoxPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pictureBoxPreview.Location = new Point(0, 0);
        pictureBoxPreview.Name = "pictureBoxPreview";
        pictureBoxPreview.Size = new Size(958, 538);
        pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxPreview.TabIndex = 0;
        pictureBoxPreview.TabStop = false;
        pictureBoxPreview.MouseDown += PictureBoxPreview_MouseDown;
        pictureBoxPreview.MouseMove += PictureBoxPreview_MouseMove;
        pictureBoxPreview.MouseUp += PictureBoxPreview_MouseUp;
        pictureBoxPreview.Paint += PictureBoxPreview_Paint;

        // btnPrevPage
        btnPrevPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnPrevPage.Location = new Point(12, 605);
        btnPrevPage.Name = "btnPrevPage";
        btnPrevPage.Size = new Size(80, 35);
        btnPrevPage.TabIndex = 3;
        btnPrevPage.Text = "< Prev";
        btnPrevPage.UseVisualStyleBackColor = true;
        btnPrevPage.Click += BtnPrevPage_Click;

        // lblPageInfo
        lblPageInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblPageInfo.Location = new Point(100, 605);
        lblPageInfo.Name = "lblPageInfo";
        lblPageInfo.Size = new Size(140, 35);
        lblPageInfo.TabIndex = 4;
        lblPageInfo.Text = "No PDF loaded";
        lblPageInfo.TextAlign = ContentAlignment.MiddleCenter;

        // btnNextPage
        btnNextPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnNextPage.Location = new Point(248, 605);
        btnNextPage.Name = "btnNextPage";
        btnNextPage.Size = new Size(80, 35);
        btnNextPage.TabIndex = 5;
        btnNextPage.Text = "Next >";
        btnNextPage.UseVisualStyleBackColor = true;
        btnNextPage.Click += BtnNextPage_Click;

        // btnSelectPage
        btnSelectPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnSelectPage.Location = new Point(348, 605);
        btnSelectPage.Name = "btnSelectPage";
        btnSelectPage.Size = new Size(120, 35);
        btnSelectPage.TabIndex = 6;
        btnSelectPage.Text = "Select This Page";
        btnSelectPage.UseVisualStyleBackColor = true;
        btnSelectPage.Click += BtnSelectPage_Click;

        // lblSelectedPage
        lblSelectedPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblSelectedPage.Location = new Point(478, 605);
        lblSelectedPage.Name = "lblSelectedPage";
        lblSelectedPage.Size = new Size(150, 35);
        lblSelectedPage.TabIndex = 7;
        lblSelectedPage.Text = "No page selected";
        lblSelectedPage.TextAlign = ContentAlignment.MiddleCenter;

        // btnInjectQR
        btnInjectQR.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnInjectQR.Location = new Point(648, 605);
        btnInjectQR.Name = "btnInjectQR";
        btnInjectQR.Size = new Size(140, 35);
        btnInjectQR.TabIndex = 8;
        btnInjectQR.Text = "Inject QR Code";
        btnInjectQR.UseVisualStyleBackColor = true;
        btnInjectQR.Click += BtnInjectQR_Click;

        // btnSavePDF
        btnSavePDF.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnSavePDF.Location = new Point(800, 605);
        btnSavePDF.Name = "btnSavePDF";
        btnSavePDF.Size = new Size(120, 35);
        btnSavePDF.TabIndex = 9;
        btnSavePDF.Text = "Save PDF";
        btnSavePDF.UseVisualStyleBackColor = true;
        btnSavePDF.Click += BtnSavePDF_Click;

        // Form1
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(984, 651);
        Controls.Add(btnSelectPDF);
        Controls.Add(lblFileName);
        Controls.Add(panelPreview);
        Controls.Add(btnPrevPage);
        Controls.Add(lblPageInfo);
        Controls.Add(btnNextPage);
        Controls.Add(btnSelectPage);
        Controls.Add(lblSelectedPage);
        Controls.Add(btnInjectQR);
        Controls.Add(btnSavePDF);
        MinimumSize = new Size(800, 600);
        Name = "Form1";
        Text = "QR Generator - PDF QR Code Injector";
        panelPreview.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private Button btnSelectPDF;
    private Label lblFileName;
    private Panel panelPreview;
    private PictureBox pictureBoxPreview;
    private Button btnPrevPage;
    private Button btnNextPage;
    private Label lblPageInfo;
    private Button btnSelectPage;
    private Label lblSelectedPage;
    private Button btnInjectQR;
    private Button btnSavePDF;
}
