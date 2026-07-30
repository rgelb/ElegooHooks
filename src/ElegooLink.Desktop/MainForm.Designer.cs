namespace ElegooLink.Desktop;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private SplitContainer _mainSplitContainer = null!;
    private TableLayoutPanel _printerLayout = null!;
    private Label _printersHeader = null!;
    private FlowLayoutPanel _printerActions = null!;
    private Button _addButton = null!;
    private Button _removeButton = null!;
    private ListView _printerList = null!;
    private ColumnHeader _printerNameColumn = null!;
    private ColumnHeader _printerStatusColumn = null!;
    private ColumnHeader _printerHostColumn = null!;
    private ContextMenuStrip _printerContextMenu = null!;
    private ToolStripMenuItem _retryMenuItem = null!;
    private ToolStripSeparator _printerContextSeparator = null!;
    private ToolStripMenuItem _removeMenuItem = null!;
    private Button _settingsButton = null!;
    private TableLayoutPanel _logLayout = null!;
    private TableLayoutPanel _logHeaderLayout = null!;
    private Label _logHeader = null!;
    private Label _logSubheader = null!;
    private FlowLayoutPanel _logActions = null!;
    private Button _clearButton = null!;
    private Button _copyButton = null!;
    private Button _minimizeToTrayButton = null!;
    private DataGridView _logGrid = null!;
    private GroupBox _detailsGroup = null!;
    private TextBox _detailsTextBox = null!;
    private ContextMenuStrip _trayContextMenu = null!;
    private ToolStripMenuItem _openTrayMenuItem = null!;
    private ToolStripSeparator _trayContextSeparator = null!;
    private ToolStripMenuItem _exitTrayMenuItem = null!;
    private NotifyIcon _trayIcon = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent() {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        _mainSplitContainer = new SplitContainer();
        _printerLayout = new TableLayoutPanel();
        _printersHeader = new Label();
        _printerActions = new FlowLayoutPanel();
        _addButton = new Button();
        _removeButton = new Button();
        _printerList = new ListView();
        _printerNameColumn = new ColumnHeader();
        _printerStatusColumn = new ColumnHeader();
        _printerHostColumn = new ColumnHeader();
        _printerContextMenu = new ContextMenuStrip(components);
        _retryMenuItem = new ToolStripMenuItem();
        _printerContextSeparator = new ToolStripSeparator();
        _removeMenuItem = new ToolStripMenuItem();
        _settingsButton = new Button();
        _logLayout = new TableLayoutPanel();
        _logHeaderLayout = new TableLayoutPanel();
        _logHeader = new Label();
        _logSubheader = new Label();
        _logActions = new FlowLayoutPanel();
        _clearButton = new Button();
        _copyButton = new Button();
        _minimizeToTrayButton = new Button();
        _logGrid = new DataGridView();
        _detailsGroup = new GroupBox();
        _detailsTextBox = new TextBox();
        _trayContextMenu = new ContextMenuStrip(components);
        _openTrayMenuItem = new ToolStripMenuItem();
        _trayContextSeparator = new ToolStripSeparator();
        _exitTrayMenuItem = new ToolStripMenuItem();
        _trayIcon = new NotifyIcon(components);
        _timestampColumn = new DataGridViewTextBoxColumn();
        _eventColumn = new DataGridViewTextBoxColumn();
        _messageColumn = new DataGridViewTextBoxColumn();
        ((System.ComponentModel.ISupportInitialize)_mainSplitContainer).BeginInit();
        _mainSplitContainer.Panel1.SuspendLayout();
        _mainSplitContainer.Panel2.SuspendLayout();
        _mainSplitContainer.SuspendLayout();
        _printerLayout.SuspendLayout();
        _printerActions.SuspendLayout();
        _printerContextMenu.SuspendLayout();
        _logLayout.SuspendLayout();
        _logHeaderLayout.SuspendLayout();
        _logActions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_logGrid).BeginInit();
        _detailsGroup.SuspendLayout();
        _trayContextMenu.SuspendLayout();
        SuspendLayout();
        // 
        // _mainSplitContainer
        // 
        _mainSplitContainer.BackColor = Color.FromArgb(215, 220, 225);
        _mainSplitContainer.Dock = DockStyle.Fill;
        _mainSplitContainer.FixedPanel = FixedPanel.Panel1;
        _mainSplitContainer.Location = new Point(0, 0);
        _mainSplitContainer.Name = "_mainSplitContainer";
        // 
        // _mainSplitContainer.Panel1
        // 
        _mainSplitContainer.Panel1.BackColor = Color.White;
        _mainSplitContainer.Panel1.Controls.Add(_printerLayout);
        _mainSplitContainer.Panel1MinSize = 260;
        // 
        // _mainSplitContainer.Panel2
        // 
        _mainSplitContainer.Panel2.BackColor = Color.White;
        _mainSplitContainer.Panel2.Controls.Add(_logLayout);
        _mainSplitContainer.Panel2MinSize = 500;
        _mainSplitContainer.Size = new Size(1834, 1090);
        _mainSplitContainer.SplitterDistance = 500;
        _mainSplitContainer.SplitterWidth = 6;
        _mainSplitContainer.TabIndex = 0;
        // 
        // _printerLayout
        // 
        _printerLayout.ColumnCount = 1;
        _printerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _printerLayout.Controls.Add(_printersHeader, 0, 0);
        _printerLayout.Controls.Add(_printerActions, 0, 1);
        _printerLayout.Controls.Add(_printerList, 0, 2);
        _printerLayout.Controls.Add(_settingsButton, 0, 3);
        _printerLayout.Dock = DockStyle.Fill;
        _printerLayout.Location = new Point(0, 0);
        _printerLayout.Name = "_printerLayout";
        _printerLayout.Padding = new Padding(14);
        _printerLayout.RowCount = 4;
        _printerLayout.RowStyles.Add(new RowStyle());
        _printerLayout.RowStyles.Add(new RowStyle());
        _printerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _printerLayout.RowStyles.Add(new RowStyle());
        _printerLayout.Size = new Size(500, 1090);
        _printerLayout.TabIndex = 0;
        // 
        // _printersHeader
        // 
        _printersHeader.AutoSize = true;
        _printersHeader.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        _printersHeader.Location = new Point(14, 14);
        _printersHeader.Margin = new Padding(0, 0, 0, 10);
        _printersHeader.Name = "_printersHeader";
        _printersHeader.Size = new Size(149, 47);
        _printersHeader.TabIndex = 0;
        _printersHeader.Text = "Printers";
        // 
        // _printerActions
        // 
        _printerActions.AutoSize = true;
        _printerActions.Controls.Add(_addButton);
        _printerActions.Controls.Add(_removeButton);
        _printerActions.Dock = DockStyle.Fill;
        _printerActions.Location = new Point(14, 71);
        _printerActions.Margin = new Padding(0, 0, 0, 10);
        _printerActions.Name = "_printerActions";
        _printerActions.Size = new Size(472, 44);
        _printerActions.TabIndex = 1;
        _printerActions.WrapContents = false;
        // 
        // _addButton
        // 
        _addButton.AutoSize = true;
        _addButton.Location = new Point(0, 0);
        _addButton.Margin = new Padding(0, 0, 8, 0);
        _addButton.Name = "_addButton";
        _addButton.Padding = new Padding(8, 2, 8, 2);
        _addButton.Size = new Size(77, 44);
        _addButton.TabIndex = 0;
        _addButton.Text = "Add";
        _addButton.UseVisualStyleBackColor = true;
        _addButton.Click += AddButton_Click;
        // 
        // _removeButton
        // 
        _removeButton.AutoSize = true;
        _removeButton.Location = new Point(85, 0);
        _removeButton.Margin = new Padding(0, 0, 8, 0);
        _removeButton.Name = "_removeButton";
        _removeButton.Padding = new Padding(8, 2, 8, 2);
        _removeButton.Size = new Size(113, 44);
        _removeButton.TabIndex = 1;
        _removeButton.Text = "Remove";
        _removeButton.UseVisualStyleBackColor = true;
        _removeButton.Click += RemoveButton_Click;
        // 
        // _printerList
        // 
        _printerList.BorderStyle = BorderStyle.FixedSingle;
        _printerList.Columns.AddRange(new ColumnHeader[] { _printerNameColumn, _printerStatusColumn, _printerHostColumn });
        _printerList.ContextMenuStrip = _printerContextMenu;
        _printerList.Dock = DockStyle.Fill;
        _printerList.FullRowSelect = true;
        _printerList.Location = new Point(17, 128);
        _printerList.MultiSelect = false;
        _printerList.Name = "_printerList";
        _printerList.Size = new Size(466, 893);
        _printerList.TabIndex = 2;
        _printerList.UseCompatibleStateImageBehavior = false;
        _printerList.View = View.Details;
        _printerList.SelectedIndexChanged += PrinterList_SelectedIndexChanged;
        _printerList.DoubleClick += PrinterList_DoubleClick;
        // 
        // _printerNameColumn
        // 
        _printerNameColumn.Text = "Printer";
        _printerNameColumn.Width = 175;
        // 
        // _printerStatusColumn
        // 
        _printerStatusColumn.Text = "Status";
        _printerStatusColumn.Width = 140;
        // 
        // _printerHostColumn
        // 
        _printerHostColumn.Text = "IP address";
        _printerHostColumn.Width = 150;
        // 
        // _printerContextMenu
        // 
        _printerContextMenu.ImageScalingSize = new Size(28, 28);
        _printerContextMenu.Items.AddRange(new ToolStripItem[] { _retryMenuItem, _printerContextSeparator, _removeMenuItem });
        _printerContextMenu.Name = "_printerContextMenu";
        _printerContextMenu.Size = new Size(243, 82);
        _printerContextMenu.Opening += PrinterContextMenu_Opening;
        // 
        // _retryMenuItem
        // 
        _retryMenuItem.Name = "_retryMenuItem";
        _retryMenuItem.Size = new Size(242, 36);
        _retryMenuItem.Text = "Retry connection";
        _retryMenuItem.Click += RetryMenuItem_Click;
        // 
        // _printerContextSeparator
        // 
        _printerContextSeparator.Name = "_printerContextSeparator";
        _printerContextSeparator.Size = new Size(239, 6);
        // 
        // _removeMenuItem
        // 
        _removeMenuItem.Name = "_removeMenuItem";
        _removeMenuItem.Size = new Size(242, 36);
        _removeMenuItem.Text = "Remove";
        _removeMenuItem.Click += RemoveButton_Click;
        // 
        // _settingsButton
        // 
        _settingsButton.AutoSize = true;
        _settingsButton.Dock = DockStyle.Bottom;
        _settingsButton.Location = new Point(14, 1036);
        _settingsButton.Margin = new Padding(0, 12, 0, 0);
        _settingsButton.Name = "_settingsButton";
        _settingsButton.Size = new Size(472, 40);
        _settingsButton.TabIndex = 3;
        _settingsButton.Text = "Event Settings...";
        _settingsButton.UseVisualStyleBackColor = true;
        _settingsButton.Click += SettingsButton_Click;
        // 
        // _logLayout
        // 
        _logLayout.ColumnCount = 1;
        _logLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _logLayout.Controls.Add(_logHeaderLayout, 0, 0);
        _logLayout.Controls.Add(_logGrid, 0, 1);
        _logLayout.Controls.Add(_detailsGroup, 0, 2);
        _logLayout.Dock = DockStyle.Fill;
        _logLayout.Location = new Point(0, 0);
        _logLayout.Name = "_logLayout";
        _logLayout.Padding = new Padding(18);
        _logLayout.RowCount = 3;
        _logLayout.RowStyles.Add(new RowStyle());
        _logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 68F));
        _logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
        _logLayout.Size = new Size(1328, 1090);
        _logLayout.TabIndex = 0;
        // 
        // _logHeaderLayout
        // 
        _logHeaderLayout.AutoSize = true;
        _logHeaderLayout.ColumnCount = 2;
        _logHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _logHeaderLayout.ColumnStyles.Add(new ColumnStyle());
        _logHeaderLayout.Controls.Add(_logHeader, 0, 0);
        _logHeaderLayout.Controls.Add(_logSubheader, 0, 1);
        _logHeaderLayout.Controls.Add(_logActions, 1, 0);
        _logHeaderLayout.Dock = DockStyle.Fill;
        _logHeaderLayout.Location = new Point(18, 18);
        _logHeaderLayout.Margin = new Padding(0, 0, 0, 12);
        _logHeaderLayout.Name = "_logHeaderLayout";
        _logHeaderLayout.RowCount = 2;
        _logHeaderLayout.RowStyles.Add(new RowStyle());
        _logHeaderLayout.RowStyles.Add(new RowStyle());
        _logHeaderLayout.Size = new Size(1292, 80);
        _logHeaderLayout.TabIndex = 0;
        // 
        // _logHeader
        // 
        _logHeader.AutoSize = true;
        _logHeader.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        _logHeader.Location = new Point(3, 0);
        _logHeader.Name = "_logHeader";
        _logHeader.Size = new Size(96, 47);
        _logHeader.TabIndex = 0;
        _logHeader.Text = "Logs";
        // 
        // _logSubheader
        // 
        _logSubheader.AutoSize = true;
        _logSubheader.ForeColor = SystemColors.GrayText;
        _logSubheader.Location = new Point(3, 50);
        _logSubheader.Margin = new Padding(3, 3, 3, 0);
        _logSubheader.Name = "_logSubheader";
        _logSubheader.Size = new Size(322, 30);
        _logSubheader.TabIndex = 1;
        _logSubheader.Text = "Select a printer to view its events.";
        // 
        // _logActions
        // 
        _logActions.Anchor = AnchorStyles.Right;
        _logActions.AutoSize = true;
        _logActions.Controls.Add(_clearButton);
        _logActions.Controls.Add(_copyButton);
        _logActions.Controls.Add(_minimizeToTrayButton);
        _logActions.FlowDirection = FlowDirection.RightToLeft;
        _logActions.Location = new Point(833, 18);
        _logActions.Name = "_logActions";
        _logHeaderLayout.SetRowSpan(_logActions, 2);
        _logActions.Size = new Size(456, 44);
        _logActions.TabIndex = 2;
        _logActions.WrapContents = false;
        // 
        // _clearButton
        // 
        _clearButton.AutoSize = true;
        _clearButton.Location = new Point(160, 0);
        _clearButton.Margin = new Padding(0, 0, 8, 0);
        _clearButton.Name = "_clearButton";
        _clearButton.Padding = new Padding(8, 2, 8, 2);
        _clearButton.Size = new Size(86, 44);
        _clearButton.TabIndex = 1;
        _clearButton.Text = "Clear";
        _clearButton.UseVisualStyleBackColor = true;
        _clearButton.Click += ClearButton_Click;
        // 
        // _copyButton
        // 
        _copyButton.AutoSize = true;
        _copyButton.Location = new Point(0, 0);
        _copyButton.Margin = new Padding(0, 0, 8, 0);
        _copyButton.Name = "_copyButton";
        _copyButton.Padding = new Padding(8, 2, 8, 2);
        _copyButton.Size = new Size(152, 44);
        _copyButton.TabIndex = 0;
        _copyButton.Text = "Copy details";
        _copyButton.UseVisualStyleBackColor = true;
        _copyButton.Click += CopyButton_Click;
        //
        // _minimizeToTrayButton
        //
        _minimizeToTrayButton.AutoSize = true;
        _minimizeToTrayButton.Location = new Point(0, 0);
        _minimizeToTrayButton.Margin = new Padding(0, 0, 8, 0);
        _minimizeToTrayButton.Name = "_minimizeToTrayButton";
        _minimizeToTrayButton.Padding = new Padding(8, 2, 8, 2);
        _minimizeToTrayButton.Size = new Size(202, 44);
        _minimizeToTrayButton.TabIndex = 2;
        _minimizeToTrayButton.Text = "Minimize to &tray";
        _minimizeToTrayButton.UseVisualStyleBackColor = true;
        _minimizeToTrayButton.Click += MinimizeToTrayButton_Click;
        // 
        // _logGrid
        // 
        _logGrid.AllowUserToAddRows = false;
        _logGrid.AllowUserToDeleteRows = false;
        _logGrid.AllowUserToResizeRows = false;
        _logGrid.BackgroundColor = SystemColors.Window;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Control;
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        _logGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        _logGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _logGrid.Columns.AddRange(new DataGridViewColumn[] { _timestampColumn, _eventColumn, _messageColumn });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Window;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        _logGrid.DefaultCellStyle = dataGridViewCellStyle2;
        _logGrid.Dock = DockStyle.Fill;
        _logGrid.Location = new Point(21, 113);
        _logGrid.MultiSelect = false;
        _logGrid.Name = "_logGrid";
        _logGrid.ReadOnly = true;
        _logGrid.RowHeadersVisible = false;
        _logGrid.RowHeadersWidth = 72;
        _logGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _logGrid.Size = new Size(1286, 648);
        _logGrid.TabIndex = 1;
        _logGrid.SelectionChanged += LogGrid_SelectionChanged;
        // 
        // _detailsGroup
        // 
        _detailsGroup.Controls.Add(_detailsTextBox);
        _detailsGroup.Dock = DockStyle.Fill;
        _detailsGroup.Location = new Point(18, 776);
        _detailsGroup.Margin = new Padding(0, 12, 0, 0);
        _detailsGroup.Name = "_detailsGroup";
        _detailsGroup.Padding = new Padding(8);
        _detailsGroup.Size = new Size(1292, 296);
        _detailsGroup.TabIndex = 2;
        _detailsGroup.TabStop = false;
        _detailsGroup.Text = "Event details";
        // 
        // _detailsTextBox
        // 
        _detailsTextBox.Dock = DockStyle.Fill;
        _detailsTextBox.Font = new Font("Consolas", 9F);
        _detailsTextBox.Location = new Point(8, 36);
        _detailsTextBox.Multiline = true;
        _detailsTextBox.Name = "_detailsTextBox";
        _detailsTextBox.ReadOnly = true;
        _detailsTextBox.ScrollBars = ScrollBars.Both;
        _detailsTextBox.Size = new Size(1276, 252);
        _detailsTextBox.TabIndex = 0;
        _detailsTextBox.WordWrap = false;
        //
        // _trayContextMenu
        //
        _trayContextMenu.ImageScalingSize = new Size(28, 28);
        _trayContextMenu.Items.AddRange(new ToolStripItem[] { _openTrayMenuItem, _trayContextSeparator, _exitTrayMenuItem });
        _trayContextMenu.Name = "_trayContextMenu";
        _trayContextMenu.Size = new Size(338, 82);
        //
        // _openTrayMenuItem
        //
        _openTrayMenuItem.Name = "_openTrayMenuItem";
        _openTrayMenuItem.Size = new Size(337, 36);
        _openTrayMenuItem.Text = "&Open Elegoo Printer Events";
        _openTrayMenuItem.Click += OpenTrayMenuItem_Click;
        //
        // _trayContextSeparator
        //
        _trayContextSeparator.Name = "_trayContextSeparator";
        _trayContextSeparator.Size = new Size(334, 6);
        //
        // _exitTrayMenuItem
        //
        _exitTrayMenuItem.Name = "_exitTrayMenuItem";
        _exitTrayMenuItem.Size = new Size(337, 36);
        _exitTrayMenuItem.Text = "E&xit";
        _exitTrayMenuItem.Click += ExitTrayMenuItem_Click;
        //
        // _trayIcon
        //
        _trayIcon.ContextMenuStrip = _trayContextMenu;
        _trayIcon.Text = "Elegoo Printer Events";
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;
        _trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;
        //
        // _timestampColumn
        // 
        _timestampColumn.HeaderText = "Time";
        _timestampColumn.MinimumWidth = 9;
        _timestampColumn.Name = "_timestampColumn";
        _timestampColumn.ReadOnly = true;
        _timestampColumn.Width = 250;
        // 
        // _eventColumn
        // 
        _eventColumn.HeaderText = "Event";
        _eventColumn.MinimumWidth = 9;
        _eventColumn.Name = "_eventColumn";
        _eventColumn.ReadOnly = true;
        _eventColumn.Width = 230;
        // 
        // _messageColumn
        // 
        _messageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _messageColumn.HeaderText = "Message";
        _messageColumn.MinimumWidth = 260;
        _messageColumn.Name = "_messageColumn";
        _messageColumn.ReadOnly = true;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(168F, 168F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(246, 248, 250);
        ClientSize = new Size(1834, 1090);
        Controls.Add(_mainSplitContainer);
        Font = new Font("Segoe UI", 9F);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(980, 620);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Elegoo Printer Events";
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;
        _mainSplitContainer.Panel1.ResumeLayout(false);
        _mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_mainSplitContainer).EndInit();
        _mainSplitContainer.ResumeLayout(false);
        _printerLayout.ResumeLayout(false);
        _printerLayout.PerformLayout();
        _printerActions.ResumeLayout(false);
        _printerActions.PerformLayout();
        _printerContextMenu.ResumeLayout(false);
        _logLayout.ResumeLayout(false);
        _logLayout.PerformLayout();
        _logHeaderLayout.ResumeLayout(false);
        _logHeaderLayout.PerformLayout();
        _logActions.ResumeLayout(false);
        _logActions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_logGrid).EndInit();
        _detailsGroup.ResumeLayout(false);
        _detailsGroup.PerformLayout();
        _trayContextMenu.ResumeLayout(false);
        ResumeLayout(false);
    }

    private DataGridViewTextBoxColumn _timestampColumn;
    private DataGridViewTextBoxColumn _eventColumn;
    private DataGridViewTextBoxColumn _messageColumn;
}
