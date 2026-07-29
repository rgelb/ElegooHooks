namespace ElegooLink.Desktop;

partial class AutomationSettingsDialog
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel _rootLayout = null!;
    private Label _instructionLabel = null!;
    private DataGridView _rulesGrid = null!;
    private GroupBox _editorGroup = null!;
    private TableLayoutPanel _editorFields = null!;
    private Label _executableLabel = null!;
    private TextBox _executableTextBox = null!;
    private Button _browseExecutableButton = null!;
    private Label _argumentsLabel = null!;
    private TextBox _argumentsTextBox = null!;
    private Label _workingDirectoryLabel = null!;
    private TextBox _workingDirectoryTextBox = null!;
    private Button _browseDirectoryButton = null!;
    private Label _placeholderLabel = null!;
    private FlowLayoutPanel _buttonPanel = null!;
    private Button _cancelButton = null!;
    private Button _saveButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent() {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutomationSettingsDialog));
        _rootLayout = new TableLayoutPanel();
        _instructionLabel = new Label();
        _rulesGrid = new DataGridView();
        _editorGroup = new GroupBox();
        _editorFields = new TableLayoutPanel();
        _executableLabel = new Label();
        _executableTextBox = new TextBox();
        _browseExecutableButton = new Button();
        _argumentsLabel = new Label();
        _argumentsTextBox = new TextBox();
        _workingDirectoryLabel = new Label();
        _workingDirectoryTextBox = new TextBox();
        _browseDirectoryButton = new Button();
        _placeholderLabel = new Label();
        _buttonPanel = new FlowLayoutPanel();
        _cancelButton = new Button();
        _saveButton = new Button();
        _enabledColumn = new DataGridViewCheckBoxColumn();
        _eventKindColumn = new DataGridViewTextBoxColumn();
        _executableColumn = new DataGridViewTextBoxColumn();
        _runHiddenColumn = new DataGridViewCheckBoxColumn();
        _rootLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_rulesGrid).BeginInit();
        _editorGroup.SuspendLayout();
        _editorFields.SuspendLayout();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _rootLayout
        // 
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_instructionLabel, 0, 0);
        _rootLayout.Controls.Add(_rulesGrid, 0, 1);
        _rootLayout.Controls.Add(_editorGroup, 0, 2);
        _rootLayout.Controls.Add(_placeholderLabel, 0, 3);
        _rootLayout.Controls.Add(_buttonPanel, 0, 4);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(16);
        _rootLayout.RowCount = 5;
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.Size = new Size(1603, 1233);
        _rootLayout.TabIndex = 0;
        // 
        // _instructionLabel
        // 
        _instructionLabel.AutoSize = true;
        _instructionLabel.Location = new Point(16, 16);
        _instructionLabel.Margin = new Padding(0, 0, 0, 10);
        _instructionLabel.Name = "_instructionLabel";
        _instructionLabel.Size = new Size(639, 30);
        _instructionLabel.TabIndex = 0;
        _instructionLabel.Text = "Choose an event, enable it, and configure the executable to launch.";
        // 
        // _rulesGrid
        // 
        _rulesGrid.AllowUserToAddRows = false;
        _rulesGrid.AllowUserToDeleteRows = false;
        _rulesGrid.AllowUserToResizeRows = false;
        _rulesGrid.BackgroundColor = SystemColors.Window;
        _rulesGrid.BorderStyle = BorderStyle.Fixed3D;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Control;
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        _rulesGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        _rulesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _rulesGrid.Columns.AddRange(new DataGridViewColumn[] { _enabledColumn, _eventKindColumn, _executableColumn, _runHiddenColumn });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Window;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        _rulesGrid.DefaultCellStyle = dataGridViewCellStyle2;
        _rulesGrid.Dock = DockStyle.Fill;
        _rulesGrid.Location = new Point(19, 59);
        _rulesGrid.MultiSelect = false;
        _rulesGrid.Name = "_rulesGrid";
        _rulesGrid.RowHeadersVisible = false;
        _rulesGrid.RowHeadersWidth = 72;
        _rulesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _rulesGrid.Size = new Size(1565, 519);
        _rulesGrid.TabIndex = 1;
        _rulesGrid.CellValueChanged += RulesGrid_CellValueChanged;
        _rulesGrid.CurrentCellDirtyStateChanged += RulesGrid_CurrentCellDirtyStateChanged;
        _rulesGrid.SelectionChanged += RulesGrid_SelectionChanged;
        // 
        // _editorGroup
        // 
        _editorGroup.Controls.Add(_editorFields);
        _editorGroup.Dock = DockStyle.Fill;
        _editorGroup.Location = new Point(16, 593);
        _editorGroup.Margin = new Padding(0, 12, 0, 0);
        _editorGroup.Name = "_editorGroup";
        _editorGroup.Padding = new Padding(12);
        _editorGroup.Size = new Size(1571, 473);
        _editorGroup.TabIndex = 2;
        _editorGroup.TabStop = false;
        _editorGroup.Text = "Selected event";
        // 
        // _editorFields
        // 
        _editorFields.ColumnCount = 3;
        _editorFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        _editorFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _editorFields.ColumnStyles.Add(new ColumnStyle());
        _editorFields.Controls.Add(_executableLabel, 0, 0);
        _editorFields.Controls.Add(_executableTextBox, 1, 0);
        _editorFields.Controls.Add(_browseExecutableButton, 2, 0);
        _editorFields.Controls.Add(_argumentsLabel, 0, 1);
        _editorFields.Controls.Add(_argumentsTextBox, 1, 1);
        _editorFields.Controls.Add(_workingDirectoryLabel, 0, 2);
        _editorFields.Controls.Add(_workingDirectoryTextBox, 1, 2);
        _editorFields.Controls.Add(_browseDirectoryButton, 2, 2);
        _editorFields.Dock = DockStyle.Fill;
        _editorFields.Location = new Point(12, 40);
        _editorFields.Name = "_editorFields";
        _editorFields.RowCount = 3;
        _editorFields.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        _editorFields.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
        _editorFields.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        _editorFields.Size = new Size(1547, 421);
        _editorFields.TabIndex = 0;
        // 
        // _executableLabel
        // 
        _executableLabel.Anchor = AnchorStyles.Left;
        _executableLabel.AutoSize = true;
        _executableLabel.Location = new Point(3, 54);
        _executableLabel.Name = "_executableLabel";
        _executableLabel.Size = new Size(113, 30);
        _executableLabel.TabIndex = 0;
        _executableLabel.Text = "Executable";
        // 
        // _executableTextBox
        // 
        _executableTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _executableTextBox.Location = new Point(128, 51);
        _executableTextBox.Name = "_executableTextBox";
        _executableTextBox.Size = new Size(1306, 35);
        _executableTextBox.TabIndex = 1;
        _executableTextBox.TextChanged += EditorTextBox_TextChanged;
        // 
        // _browseExecutableButton
        // 
        _browseExecutableButton.Anchor = AnchorStyles.Left;
        _browseExecutableButton.AutoSize = true;
        _browseExecutableButton.Location = new Point(1440, 49);
        _browseExecutableButton.Name = "_browseExecutableButton";
        _browseExecutableButton.Size = new Size(104, 40);
        _browseExecutableButton.TabIndex = 2;
        _browseExecutableButton.Text = "Browse...";
        _browseExecutableButton.UseVisualStyleBackColor = true;
        _browseExecutableButton.Click += BrowseExecutable_Click;
        // 
        // _argumentsLabel
        // 
        _argumentsLabel.Anchor = AnchorStyles.Left;
        _argumentsLabel.AutoSize = true;
        _argumentsLabel.Location = new Point(3, 194);
        _argumentsLabel.Name = "_argumentsLabel";
        _argumentsLabel.Size = new Size(115, 30);
        _argumentsLabel.TabIndex = 3;
        _argumentsLabel.Text = "Arguments";
        // 
        // _argumentsTextBox
        // 
        _argumentsTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _editorFields.SetColumnSpan(_argumentsTextBox, 2);
        _argumentsTextBox.Location = new Point(128, 192);
        _argumentsTextBox.Name = "_argumentsTextBox";
        _argumentsTextBox.Size = new Size(1416, 35);
        _argumentsTextBox.TabIndex = 4;
        _argumentsTextBox.TextChanged += EditorTextBox_TextChanged;
        // 
        // _workingDirectoryLabel
        // 
        _workingDirectoryLabel.Anchor = AnchorStyles.Left;
        _workingDirectoryLabel.AutoSize = true;
        _workingDirectoryLabel.Location = new Point(3, 321);
        _workingDirectoryLabel.Name = "_workingDirectoryLabel";
        _workingDirectoryLabel.Size = new Size(96, 60);
        _workingDirectoryLabel.TabIndex = 5;
        _workingDirectoryLabel.Text = "Working directory";
        // 
        // _workingDirectoryTextBox
        // 
        _workingDirectoryTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _workingDirectoryTextBox.Location = new Point(128, 333);
        _workingDirectoryTextBox.Name = "_workingDirectoryTextBox";
        _workingDirectoryTextBox.Size = new Size(1306, 35);
        _workingDirectoryTextBox.TabIndex = 6;
        _workingDirectoryTextBox.TextChanged += EditorTextBox_TextChanged;
        // 
        // _browseDirectoryButton
        // 
        _browseDirectoryButton.Anchor = AnchorStyles.Left;
        _browseDirectoryButton.AutoSize = true;
        _browseDirectoryButton.Location = new Point(1440, 331);
        _browseDirectoryButton.Name = "_browseDirectoryButton";
        _browseDirectoryButton.Size = new Size(104, 40);
        _browseDirectoryButton.TabIndex = 7;
        _browseDirectoryButton.Text = "Browse...";
        _browseDirectoryButton.UseVisualStyleBackColor = true;
        _browseDirectoryButton.Click += BrowseDirectory_Click;
        // 
        // _placeholderLabel
        // 
        _placeholderLabel.AutoSize = true;
        _placeholderLabel.ForeColor = SystemColors.GrayText;
        _placeholderLabel.Location = new Point(16, 1076);
        _placeholderLabel.Margin = new Padding(0, 10, 0, 10);
        _placeholderLabel.MaximumSize = new Size(880, 0);
        _placeholderLabel.Name = "_placeholderLabel";
        _placeholderLabel.Size = new Size(852, 90);
        _placeholderLabel.TabIndex = 3;
        _placeholderLabel.Text = resources.GetString("_placeholderLabel.Text");
        // 
        // _buttonPanel
        // 
        _buttonPanel.AutoSize = true;
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Controls.Add(_saveButton);
        _buttonPanel.Dock = DockStyle.Fill;
        _buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        _buttonPanel.Location = new Point(16, 1176);
        _buttonPanel.Margin = new Padding(0);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new Size(1571, 41);
        _buttonPanel.TabIndex = 4;
        _buttonPanel.WrapContents = false;
        // 
        // _cancelButton
        // 
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(1486, 0);
        _cancelButton.Margin = new Padding(8, 0, 0, 0);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(85, 40);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Cancel";
        _cancelButton.UseVisualStyleBackColor = true;
        // 
        // _saveButton
        // 
        _saveButton.AutoSize = true;
        _saveButton.Location = new Point(1403, 0);
        _saveButton.Margin = new Padding(0);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(75, 40);
        _saveButton.TabIndex = 0;
        _saveButton.Text = "Save";
        _saveButton.UseVisualStyleBackColor = true;
        _saveButton.Click += SaveButton_Click;
        // 
        // _enabledColumn
        // 
        _enabledColumn.HeaderText = "Enabled";
        _enabledColumn.MinimumWidth = 9;
        _enabledColumn.Name = "_enabledColumn";
        // 
        // _eventKindColumn
        // 
        _eventKindColumn.HeaderText = "Event";
        _eventKindColumn.MinimumWidth = 9;
        _eventKindColumn.Name = "_eventKindColumn";
        _eventKindColumn.ReadOnly = true;
        _eventKindColumn.Width = 200;
        // 
        // _executableColumn
        // 
        _executableColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _executableColumn.HeaderText = "Executable";
        _executableColumn.MinimumWidth = 220;
        _executableColumn.Name = "_executableColumn";
        _executableColumn.ReadOnly = true;
        // 
        // _runHiddenColumn
        // 
        _runHiddenColumn.HeaderText = "Run hidden";
        _runHiddenColumn.MinimumWidth = 9;
        _runHiddenColumn.Name = "_runHiddenColumn";
        _runHiddenColumn.Width = 90;
        // 
        // AutomationSettingsDialog
        // 
        AcceptButton = _saveButton;
        AutoScaleDimensions = new SizeF(168F, 168F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _cancelButton;
        ClientSize = new Size(1603, 1233);
        Controls.Add(_rootLayout);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon")!;
        MinimizeBox = false;
        MinimumSize = new Size(780, 560);
        Name = "AutomationSettingsDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Event Actions";
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_rulesGrid).EndInit();
        _editorGroup.ResumeLayout(false);
        _editorFields.ResumeLayout(false);
        _editorFields.PerformLayout();
        _buttonPanel.ResumeLayout(false);
        _buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    private DataGridViewCheckBoxColumn _enabledColumn;
    private DataGridViewTextBoxColumn _eventKindColumn;
    private DataGridViewTextBoxColumn _executableColumn;
    private DataGridViewCheckBoxColumn _runHiddenColumn;
}
