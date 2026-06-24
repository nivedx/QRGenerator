namespace QRGenerator;

public class SettingsForm : Form
{
    private readonly TextBox _txtTenantId;
    private readonly TextBox _txtClientId;
    private readonly TextBox _txtClientSecret;
    private readonly TextBox _txtUserEmail;
    private readonly TextBox _txtTargetFolder;

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public OneDriveSettings? Result { get; private set; }

    public SettingsForm(OneDriveSettings current)
    {
        Text            = "OneDrive Settings";
        ClientSize      = new Size(520, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Font            = new Font("Segoe UI", 9f);

        const int lx = 12, lw = 162, tx = 180, tw = 324, rh = 24, gap = 10;
        int y = 16;

        // ── Tenant ID ─────────────────────────────────────────────────────
        Controls.Add(new Label { Text = "Tenant ID *", Left = lx, Top = y + 3, Width = lw });
        _txtTenantId = new TextBox { Left = tx, Top = y, Width = tw, Text = current.TenantId };
        Controls.Add(_txtTenantId);
        y += rh + gap;

        // ── Client ID ─────────────────────────────────────────────────────
        Controls.Add(new Label { Text = "Client ID *", Left = lx, Top = y + 3, Width = lw });
        _txtClientId = new TextBox { Left = tx, Top = y, Width = tw, Text = current.ClientId };
        Controls.Add(_txtClientId);
        y += rh + gap;

        // ── Client Secret ─────────────────────────────────────────────────
        Controls.Add(new Label { Text = "Client Secret *", Left = lx, Top = y + 3, Width = lw });
        _txtClientSecret = new TextBox
        {
            Left = tx, Top = y, Width = tw,
            Text = current.ClientSecret,
            UseSystemPasswordChar = true
        };
        Controls.Add(_txtClientSecret);
        y += rh + gap;

        var chk = new CheckBox
        {
            Text = "Show secret", Left = tx, Top = y, AutoSize = true,
            Font = new Font("Segoe UI", 8f)
        };
        chk.CheckedChanged += (_, _) => _txtClientSecret.UseSystemPasswordChar = !chk.Checked;
        Controls.Add(chk);
        y += 20 + gap;

        // ── User Email ────────────────────────────────────────────────────
        Controls.Add(new Label { Text = "User Email / UPN *", Left = lx, Top = y + 3, Width = lw });
        _txtUserEmail = new TextBox { Left = tx, Top = y, Width = tw, Text = current.UserEmail };
        Controls.Add(_txtUserEmail);
        y += rh + gap;

        // ── Target Folder ─────────────────────────────────────────────────
        Controls.Add(new Label { Text = "Target Folder", Left = lx, Top = y + 3, Width = lw });
        _txtTargetFolder = new TextBox { Left = tx, Top = y, Width = tw, Text = current.TargetFolder };
        Controls.Add(_txtTargetFolder);
        Controls.Add(new Label
        {
            Text = "Leave blank to upload to OneDrive root.",
            Left = tx, Top = y + rh + 2, Width = tw, Height = 14,
            Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray, AutoSize = false
        });
        y += rh + 18 + gap;

        // ── Buttons ───────────────────────────────────────────────────────
        var btnSave = new Button { Text = "Save", Left = tx, Top = y, Width = 90, Height = 30 };
        var btnCancel = new Button
        {
            Text = "Cancel", Left = tx + 100, Top = y, Width = 80, Height = 30,
            DialogResult = DialogResult.Cancel
        };

        btnSave.Click += (_, _) =>
        {
            if (!ValidateInputs()) return;
            Result = new OneDriveSettings
            {
                TenantId     = _txtTenantId.Text.Trim(),
                ClientId     = _txtClientId.Text.Trim(),
                ClientSecret = _txtClientSecret.Text.Trim(),
                UserEmail    = _txtUserEmail.Text.Trim(),
                TargetFolder = _txtTargetFolder.Text.Trim()
            };
            Result.Save();
            DialogResult = DialogResult.OK;
        };

        Controls.Add(btnSave);
        Controls.Add(btnCancel);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private bool ValidateInputs()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(_txtTenantId.Text))     errors.Add("• Tenant ID is required.");
        if (string.IsNullOrWhiteSpace(_txtClientId.Text))     errors.Add("• Client ID is required.");
        if (string.IsNullOrWhiteSpace(_txtClientSecret.Text)) errors.Add("• Client Secret is required.");
        if (string.IsNullOrWhiteSpace(_txtUserEmail.Text))    errors.Add("• User Email / UPN is required.");

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Missing Fields",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }
}
