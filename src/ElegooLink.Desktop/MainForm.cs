using ElegooLink.Desktop.Core;

namespace ElegooLink.Desktop;

internal sealed partial class MainForm : Form
{
    private readonly Lazy<PrinterMonitorController> _controller =
        new(static () => new PrinterMonitorController());
    private readonly CancellationTokenSource _lifetime = new();
    private bool _startupComplete;
    private bool _closing;
    private bool _allowClose;

    public MainForm() {
        InitializeComponent();
        if (System.ComponentModel.LicenseManager.UsageMode !=
            System.ComponentModel.LicenseUsageMode.Designtime) {
            SubscribeToController();
            UpdateSelectionState();
        }
    }

    private PrinterMonitorController Controller => _controller.Value;

    private void SubscribeToController() {
        Controller.PrintersChanged += (_, _) => RunOnUi(UpdatePrinterList);
        Controller.LogEntryAdded += (_, eventArgs) => RunOnUi(() => {
            if (SelectedPrinterId() == eventArgs.PrinterId) {
                AddLogRow(eventArgs.Entry);
            }
        });
        Controller.WarningRaised += (_, eventArgs) => RunOnUi(() =>
            MessageBox.Show(
                this,
                eventArgs.Message,
                "Settings warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning));
    }

    private async void MainForm_Shown(object? sender, EventArgs eventArgs) {
        SetUiBusy(true);
        try {
            await Controller.StartAsync(_lifetime.Token);
            _startupComplete = true;
            UpdatePrinterList();
        } catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
            // Window shutdown canceled startup.
        } catch (Exception exception) {
            MessageBox.Show(
                this,
                exception.Message,
                "Elegoo Link could not start",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            BeginInvoke(Close);
        } finally {
            if (!_closing) {
                SetUiBusy(false);
            }
        }
    }

    private async void MainForm_FormClosing(
        object? sender,
        FormClosingEventArgs eventArgs) {
        if (_allowClose) {
            return;
        }

        eventArgs.Cancel = true;
        if (_closing) {
            return;
        }

        _closing = true;
        _lifetime.Cancel();
        SetUiBusy(true);
        _logSubheader.Text = "Disconnecting printers...";

        try {
            if (_controller.IsValueCreated) {
                await Controller.ShutdownAsync();
            }
        } catch (Exception exception) {
            MessageBox.Show(
                this,
                $"The application encountered an error while closing:{Environment.NewLine}{exception.Message}",
                "Shutdown warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        } finally {
            _lifetime.Dispose();
            _allowClose = true;
            Close();
        }
    }

    private async void AddButton_Click(object? sender, EventArgs eventArgs) {
        using var dialog = new AddPrinterDialog(
            Controller.DiscoverByHostAsync,
            Controller.IsDuplicateHost);
        if (dialog.ShowDialog(this) != DialogResult.OK ||
            dialog.Request is null) {
            return;
        }

        SetUiBusy(true);
        try {
            var printerId = await Controller.AddPrinterAsync(
                dialog.Request,
                _lifetime.Token);
            SelectPrinter(printerId);
        } catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
        } catch (Exception exception) {
            ShowOperationError("The printer could not be added.", exception);
        } finally {
            if (!_closing) {
                SetUiBusy(false);
            }
        }
    }

    private async void RemoveButton_Click(object? sender, EventArgs eventArgs) {
        var selected = SelectedPrinter();
        if (selected is null) {
            return;
        }

        if (MessageBox.Show(
                this,
                $"Remove '{selected.DisplayName}' and its session logs?",
                "Remove printer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) {
            return;
        }

        SetUiBusy(true);
        try {
            await Controller.RemovePrinterAsync(selected.Id, _lifetime.Token);
        } catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
        } catch (Exception exception) {
            ShowOperationError("The printer could not be removed.", exception);
        } finally {
            if (!_closing) {
                SetUiBusy(false);
            }
        }
    }

    private async void SettingsButton_Click(object? sender, EventArgs eventArgs) {
        using var dialog = new AutomationSettingsDialog(
            Controller.GetAutomationRules());
        if (dialog.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        SetUiBusy(true);
        try {
            await Controller.UpdateAutomationRulesAsync(
                dialog.Rules,
                _lifetime.Token);
        } catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
        } catch (Exception exception) {
            ShowOperationError("The event settings could not be saved.", exception);
        } finally {
            if (!_closing) {
                SetUiBusy(false);
            }
        }
    }

    private void PrinterList_SelectedIndexChanged(
        object? sender,
        EventArgs eventArgs) {
        UpdateSelectionState();
        RefreshSelectedLogs();
    }

    private async void PrinterList_DoubleClick(object? sender, EventArgs eventArgs) {
        if (SelectedPrinter() is { Status: PrinterConnectionStatus.Offline }) {
            await RetrySelectedPrinterAsync();
        }
    }

    private async void RetryMenuItem_Click(object? sender, EventArgs eventArgs) =>
        await RetrySelectedPrinterAsync();

    private void PrinterContextMenu_Opening(
        object? sender,
        System.ComponentModel.CancelEventArgs eventArgs) {
        var selected = SelectedPrinter();
        if (selected is null) {
            eventArgs.Cancel = true;
            return;
        }

        _retryMenuItem.Enabled =
            selected.Status == PrinterConnectionStatus.Offline;
    }

    private void LogGrid_SelectionChanged(object? sender, EventArgs eventArgs) {
        _detailsTextBox.Text = SelectedLogEntry()?.Details ?? "";
        _copyButton.Enabled = SelectedLogEntry() is not null;
    }

    private async Task RetrySelectedPrinterAsync() {
        var selected = SelectedPrinter();
        if (selected is null) {
            return;
        }

        SetUiBusy(true);
        try {
            await Controller.RetryConnectionAsync(
                selected.Id,
                _lifetime.Token);
        } catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) {
        } catch (Exception exception) {
            ShowOperationError("The connection could not be retried.", exception);
        } finally {
            if (!_closing) {
                SetUiBusy(false);
            }
        }
    }

    private void ClearButton_Click(object? sender, EventArgs eventArgs) {
        if (SelectedPrinterId() is not { } printerId) {
            return;
        }

        Controller.ClearLogs(printerId);
        RefreshSelectedLogs();
    }

    private void CopyButton_Click(object? sender, EventArgs eventArgs) {
        if (SelectedLogEntry() is { } entry) {
            Clipboard.SetText(
                $"[{FormatLocalTimestamp(entry.TimestampUtc)}] {entry.EventName}: {entry.Message}" +
                $"{Environment.NewLine}{Environment.NewLine}{entry.Details}");
        }
    }

    private void UpdatePrinterList() {
        var selectedId = SelectedPrinterId();
        var printers = Controller.GetPrinters();

        _printerList.BeginUpdate();
        try {
            _printerList.Items.Clear();
            foreach (var printer in printers) {
                var item = new ListViewItem(printer.DisplayName) {
                    Tag = printer.Id,
                    ForeColor = StatusColor(printer.Status)
                };
                item.SubItems.Add(StatusText(printer.Status));
                item.SubItems.Add(printer.Host);
                _printerList.Items.Add(item);
            }
        } finally {
            _printerList.EndUpdate();
        }

        if (selectedId is { } previous) {
            SelectPrinter(previous);
        }

        if (_printerList.SelectedItems.Count == 0 &&
            _printerList.Items.Count > 0) {
            _printerList.Items[0].Selected = true;
        }

        UpdateSelectionState();
    }

    private void RefreshSelectedLogs() {
        _logGrid.Rows.Clear();
        _detailsTextBox.Clear();

        var selected = SelectedPrinter();
        if (selected is null) {
            _logHeader.Text = "Logs";
            _logSubheader.Text = "Select a printer to view its events.";
            UpdateSelectionState();
            return;
        }

        _logHeader.Text = selected.DisplayName;
        _logSubheader.Text =
            $"{selected.Host}  •  {StatusText(selected.Status)}  •  {selected.PrinterType}";
        foreach (var entry in Controller.GetLogs(selected.Id)) {
            AddLogRow(entry);
        }

        UpdateSelectionState();
    }

    private void AddLogRow(PrinterLogEntry entry) {
        var followTail =
            _logGrid.Rows.Count == 0 ||
            _logGrid.FirstDisplayedScrollingRowIndex < 0 ||
            _logGrid.FirstDisplayedScrollingRowIndex +
            _logGrid.DisplayedRowCount(includePartialRow: true) >=
            _logGrid.Rows.Count - 1;

        if (_logGrid.Rows.Count >= 10_000) {
            _logGrid.Rows.RemoveAt(0);
        }

        var rowIndex = _logGrid.Rows.Add(
            FormatLocalTimestamp(entry.TimestampUtc),
            entry.EventName,
            entry.Message);
        var row = _logGrid.Rows[rowIndex];
        row.Tag = entry;
        if (entry.IsApplicationEvent) {
            row.DefaultCellStyle.ForeColor = Color.FromArgb(70, 88, 108);
        }

        if (rowIndex == 0) {
            row.Selected = true;
        }

        if (followTail && rowIndex >= 0) {
            var visibleRows = Math.Max(
                1,
                _logGrid.DisplayedRowCount(includePartialRow: true));
            _logGrid.FirstDisplayedScrollingRowIndex =
                Math.Max(0, rowIndex - visibleRows + 1);
        }
    }

    private void UpdateSelectionState() {
        var selected = SelectedPrinter();
        var hasSelection = selected is not null;
        _removeButton.Enabled = _startupComplete && !_closing && hasSelection;
        _removeMenuItem.Enabled = hasSelection;
        _retryMenuItem.Enabled =
            hasSelection && selected!.Status == PrinterConnectionStatus.Offline;
        _clearButton.Enabled = hasSelection;
        _copyButton.Enabled = SelectedLogEntry() is not null;
    }

    private void SetUiBusy(bool busy) {
        UseWaitCursor = busy;
        _addButton.Enabled = !busy && _startupComplete;
        _removeButton.Enabled =
            !busy && _startupComplete && SelectedPrinterId() is not null;
        _settingsButton.Enabled = !busy && _startupComplete;
        _printerList.Enabled = !busy;
    }

    private PrinterView? SelectedPrinter() {
        var selectedId = SelectedPrinterId();
        return selectedId is null
            ? null
            : Controller.GetPrinters().FirstOrDefault(
                printer => printer.Id == selectedId);
    }

    private Guid? SelectedPrinterId() =>
        _printerList.SelectedItems.Count == 0
            ? null
            : (Guid?)_printerList.SelectedItems[0].Tag;

    private PrinterLogEntry? SelectedLogEntry() =>
        _logGrid.SelectedRows.Count == 0
            ? null
            : _logGrid.SelectedRows[0].Tag as PrinterLogEntry;

    private void SelectPrinter(Guid printerId) {
        foreach (ListViewItem item in _printerList.Items) {
            if (item.Tag is Guid id && id == printerId) {
                item.Selected = true;
                item.EnsureVisible();
                return;
            }
        }
    }

    private void RunOnUi(Action action) {
        if (IsDisposed || Disposing) {
            return;
        }

        if (InvokeRequired) {
            try {
                BeginInvoke(action);
            } catch (InvalidOperationException) {
                // The window handle was destroyed during shutdown.
            }
        } else {
            action();
        }
    }

    private void ShowOperationError(string message, Exception exception) =>
        MessageBox.Show(
            this,
            $"{message}{Environment.NewLine}{Environment.NewLine}{exception.Message}",
            "Elegoo Printer Events",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

    private static string StatusText(PrinterConnectionStatus status) =>
        status switch {
            PrinterConnectionStatus.Connected => "Connected",
            PrinterConnectionStatus.Connecting => "Connecting",
            _ => "Offline"
        };

    private static Color StatusColor(PrinterConnectionStatus status) =>
        status switch {
            PrinterConnectionStatus.Connected => Color.FromArgb(28, 128, 72),
            PrinterConnectionStatus.Connecting => Color.FromArgb(176, 112, 20),
            _ => SystemColors.GrayText
        };

    private static string FormatLocalTimestamp(DateTimeOffset timestampUtc) =>
        timestampUtc
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
}
