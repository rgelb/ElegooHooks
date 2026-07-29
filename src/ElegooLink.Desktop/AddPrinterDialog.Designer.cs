namespace ElegooLink.Desktop;

partial class AddPrinterDialog
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel _rootLayout = null!;
    private Label _titleLabel = null!;
    private Label _hostLabel = null!;
    private TextBox _hostTextBox = null!;
    private Button _advancedButton = null!;
    private Panel _advancedPanel = null!;
    private TableLayoutPanel _advancedFields = null!;
    private Label _nameLabel = null!;
    private TextBox _nameTextBox = null!;
    private Label _typeLabel = null!;
    private ComboBox _typeComboBox = null!;
    private Label _errorLabel = null!;
    private FlowLayoutPanel _buttonPanel = null!;
    private Button _cancelButton = null!;
    private Button _addButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent() {
        _rootLayout = new TableLayoutPanel();
        _titleLabel = new Label();
        _hostLabel = new Label();
        _hostTextBox = new TextBox();
        _advancedButton = new Button();
        _advancedPanel = new Panel();
        _advancedFields = new TableLayoutPanel();
        _nameLabel = new Label();
        _nameTextBox = new TextBox();
        _typeLabel = new Label();
        _typeComboBox = new ComboBox();
        _errorLabel = new Label();
        _buttonPanel = new FlowLayoutPanel();
        _cancelButton = new Button();
        _addButton = new Button();
        _rootLayout.SuspendLayout();
        _advancedPanel.SuspendLayout();
        _advancedFields.SuspendLayout();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _rootLayout
        // 
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_titleLabel, 0, 0);
        _rootLayout.Controls.Add(_hostLabel, 0, 1);
        _rootLayout.Controls.Add(_hostTextBox, 0, 2);
        _rootLayout.Controls.Add(_advancedButton, 0, 3);
        _rootLayout.Controls.Add(_advancedPanel, 0, 4);
        _rootLayout.Controls.Add(_errorLabel, 0, 5);
        _rootLayout.Controls.Add(_buttonPanel, 0, 6);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(18);
        _rootLayout.RowCount = 7;
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.Size = new Size(500, 238);
        _rootLayout.TabIndex = 0;
        // 
        // _titleLabel
        // 
        _titleLabel.AutoSize = true;
        _titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _titleLabel.Location = new Point(18, 18);
        _titleLabel.Margin = new Padding(0, 0, 0, 12);
        _titleLabel.Name = "_titleLabel";
        _titleLabel.Size = new Size(347, 30);
        _titleLabel.TabIndex = 0;
        _titleLabel.Text = "Connect to a printer by IP address";
        // 
        // _hostLabel
        // 
        _hostLabel.AutoSize = true;
        _hostLabel.Location = new Point(18, 60);
        _hostLabel.Margin = new Padding(0, 0, 0, 4);
        _hostLabel.Name = "_hostLabel";
        _hostLabel.Size = new Size(108, 30);
        _hostLabel.TabIndex = 1;
        _hostLabel.Text = "IP address";
        // 
        // _hostTextBox
        // 
        _hostTextBox.Dock = DockStyle.Top;
        _hostTextBox.Location = new Point(18, 94);
        _hostTextBox.Margin = new Padding(0, 0, 0, 8);
        _hostTextBox.Name = "_hostTextBox";
        _hostTextBox.PlaceholderText = "192.168.1.42";
        _hostTextBox.Size = new Size(464, 35);
        _hostTextBox.TabIndex = 2;
        // 
        // _advancedButton
        // 
        _advancedButton.AutoSize = true;
        _advancedButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _advancedButton.FlatAppearance.BorderSize = 0;
        _advancedButton.FlatStyle = FlatStyle.Flat;
        _advancedButton.Location = new Point(18, 137);
        _advancedButton.Margin = new Padding(0);
        _advancedButton.Name = "_advancedButton";
        _advancedButton.Padding = new Padding(0, 2, 4, 2);
        _advancedButton.Size = new Size(149, 44);
        _advancedButton.TabIndex = 3;
        _advancedButton.Text = "▶  Advanced";
        _advancedButton.TextAlign = ContentAlignment.MiddleLeft;
        _advancedButton.UseVisualStyleBackColor = true;
        _advancedButton.Click += AdvancedButton_Click;
        // 
        // _advancedPanel
        // 
        _advancedPanel.Controls.Add(_advancedFields);
        _advancedPanel.Dock = DockStyle.Fill;
        _advancedPanel.Location = new Point(18, 187);
        _advancedPanel.Margin = new Padding(0, 6, 0, 0);
        _advancedPanel.Name = "_advancedPanel";
        _advancedPanel.Size = new Size(464, 1);
        _advancedPanel.TabIndex = 4;
        _advancedPanel.Visible = false;
        // 
        // _advancedFields
        // 
        _advancedFields.ColumnCount = 2;
        _advancedFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _advancedFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _advancedFields.Controls.Add(_nameLabel, 0, 0);
        _advancedFields.Controls.Add(_nameTextBox, 1, 0);
        _advancedFields.Controls.Add(_typeLabel, 0, 1);
        _advancedFields.Controls.Add(_typeComboBox, 1, 1);
        _advancedFields.Dock = DockStyle.Fill;
        _advancedFields.Location = new Point(0, 0);
        _advancedFields.Name = "_advancedFields";
        _advancedFields.RowCount = 2;
        _advancedFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _advancedFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _advancedFields.Size = new Size(464, 1);
        _advancedFields.TabIndex = 0;
        // 
        // _nameLabel
        // 
        _nameLabel.Anchor = AnchorStyles.Left;
        _nameLabel.AutoSize = true;
        _nameLabel.Location = new Point(3, 0);
        _nameLabel.Name = "_nameLabel";
        _nameLabel.Size = new Size(91, 36);
        _nameLabel.TabIndex = 0;
        _nameLabel.Text = "Friendly name";
        // 
        // _nameTextBox
        // 
        _nameTextBox.Dock = DockStyle.Fill;
        _nameTextBox.Location = new Point(123, 3);
        _nameTextBox.Name = "_nameTextBox";
        _nameTextBox.PlaceholderText = "Optional";
        _nameTextBox.Size = new Size(338, 35);
        _nameTextBox.TabIndex = 1;
        // 
        // _typeLabel
        // 
        _typeLabel.Anchor = AnchorStyles.Left;
        _typeLabel.AutoSize = true;
        _typeLabel.Location = new Point(3, 36);
        _typeLabel.Name = "_typeLabel";
        _typeLabel.Size = new Size(80, 36);
        _typeLabel.TabIndex = 2;
        _typeLabel.Text = "Printer type";
        // 
        // _typeComboBox
        // 
        _typeComboBox.Dock = DockStyle.Fill;
        _typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _typeComboBox.FormattingEnabled = true;
        _typeComboBox.Location = new Point(123, 39);
        _typeComboBox.Name = "_typeComboBox";
        _typeComboBox.Size = new Size(338, 38);
        _typeComboBox.TabIndex = 3;
        // 
        // _errorLabel
        // 
        _errorLabel.Dock = DockStyle.Fill;
        _errorLabel.ForeColor = Color.Firebrick;
        _errorLabel.Location = new Point(18, 187);
        _errorLabel.Margin = new Padding(0, 6, 0, 6);
        _errorLabel.Name = "_errorLabel";
        _errorLabel.Size = new Size(464, 1);
        _errorLabel.TabIndex = 5;
        _errorLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _buttonPanel
        // 
        _buttonPanel.AutoSize = true;
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Controls.Add(_addButton);
        _buttonPanel.Dock = DockStyle.Fill;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.Location = new Point(18, 180);
        _buttonPanel.Margin = new Padding(0);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new Size(464, 40);
        _buttonPanel.TabIndex = 6;
        _buttonPanel.WrapContents = false;
        // 
        // _cancelButton
        // 
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(379, 0);
        _cancelButton.Margin = new Padding(8, 0, 0, 0);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(85, 40);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Cancel";
        _cancelButton.UseVisualStyleBackColor = true;
        // 
        // _addButton
        // 
        _addButton.AutoSize = true;
        _addButton.Location = new Point(296, 0);
        _addButton.Margin = new Padding(0);
        _addButton.Name = "_addButton";
        _addButton.Size = new Size(75, 40);
        _addButton.TabIndex = 0;
        _addButton.Text = "Add";
        _addButton.UseVisualStyleBackColor = true;
        _addButton.Click += AddButton_Click;
        // 
        // AddPrinterDialog
        // 
        AcceptButton = _addButton;
        AutoScaleDimensions = new SizeF(168F, 168F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _cancelButton;
        ClientSize = new Size(500, 238);
        Controls.Add(_rootLayout);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AddPrinterDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Add Printer";
        FormClosed += AddPrinterDialog_FormClosed;
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        _advancedPanel.ResumeLayout(false);
        _advancedFields.ResumeLayout(false);
        _advancedFields.PerformLayout();
        _buttonPanel.ResumeLayout(false);
        _buttonPanel.PerformLayout();
        ResumeLayout(false);
    }
}
