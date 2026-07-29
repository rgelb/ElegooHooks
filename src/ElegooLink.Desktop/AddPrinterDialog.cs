using ElegooLink.Desktop.Core;
using ElegooLink.Events;

namespace ElegooLink.Desktop;

internal sealed partial class AddPrinterDialog : Form
{
    private const int CollapsedHeight = 238;
    private const int ExpandedHeight = 364;

    private Func<string, CancellationToken, Task<PrinterInfo?>> _discover =
        static (_, _) => Task.FromResult<PrinterInfo?>(null);
    private Func<string, bool> _isDuplicateHost = static _ => false;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _advancedVisible;

    public AddPrinterDialog()
    {
        InitializeComponent();
    }

    public AddPrinterDialog(
        Func<string, CancellationToken, Task<PrinterInfo?>> discover,
        Func<string, bool> isDuplicateHost)
        : this()
    {
        _discover = discover;
        _isDuplicateHost = isDuplicateHost;
        PopulatePrinterTypes();
    }

    public NewPrinterRequest? Request { get; private set; }

    private void PopulatePrinterTypes()
    {
        _typeComboBox.Items.AddRange(
        [
            new PrinterTypeChoice("Auto Detect", null),
            new PrinterTypeChoice(
                "ELEGOO FDM / Klipper",
                PrinterType.ElegooFdmKlipper),
            new PrinterTypeChoice(
                "Centauri Carbon",
                PrinterType.ElegooCentauriCarbon),
            new PrinterTypeChoice(
                "Centauri Carbon 2",
                PrinterType.ElegooCentauriCarbon2),
            new PrinterTypeChoice(
                "Generic FDM / Klipper",
                PrinterType.GenericFdmKlipper)
        ]);
        _typeComboBox.SelectedIndex = 0;
    }

    private void AdvancedButton_Click(object? sender, EventArgs eventArgs) =>
        SetAdvancedVisible(!_advancedVisible);

    private void AddPrinterDialog_FormClosed(
        object? sender,
        FormClosedEventArgs eventArgs) =>
        _lifetime.Cancel();

    private async void AddButton_Click(object? sender, EventArgs eventArgs)
    {
        _errorLabel.Text = "";
        if (!PrinterAddress.TryNormalize(_hostTextBox.Text, out var host))
        {
            _errorLabel.Text = "Enter a valid IPv4 or IPv6 address.";
            _hostTextBox.Focus();
            return;
        }

        if (_isDuplicateHost(host))
        {
            _errorLabel.Text = $"A printer at {host} is already in the list.";
            _hostTextBox.Focus();
            return;
        }

        if (_typeComboBox.SelectedItem is not PrinterTypeChoice choice)
        {
            _errorLabel.Text = "Select a printer type.";
            return;
        }

        PrinterInfo? discovered = null;
        var printerType = choice.PrinterType;

        if (printerType is null)
        {
            SetBusy(true, "Looking for the printer on the local network...");
            try
            {
                discovered = await _discover(host, _lifetime.Token);
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                SetAdvancedVisible(true);
                _errorLabel.Text =
                    $"Auto-detect failed: {exception.Message} Select a printer type to save it offline.";
                return;
            }
            finally
            {
                SetBusy(false);
            }

            if (discovered is null ||
                discovered.PrinterType == PrinterType.Unknown)
            {
                SetAdvancedVisible(true);
                _errorLabel.Text =
                    "The printer was not detected. Select its type under Advanced to save it offline.";
                _typeComboBox.Focus();
                return;
            }

            printerType = discovered.PrinterType;
        }

        Request = new NewPrinterRequest(
            host,
            _nameTextBox.Text.Trim(),
            printerType.Value,
            discovered);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _addButton.Enabled = !busy;
        _hostTextBox.Enabled = !busy;
        _advancedButton.Enabled = !busy;
        _advancedPanel.Enabled = !busy;
        UseWaitCursor = busy;
        if (!string.IsNullOrWhiteSpace(status))
        {
            _errorLabel.ForeColor = SystemColors.ControlText;
            _errorLabel.Text = status;
        }
        else
        {
            _errorLabel.ForeColor = Color.Firebrick;
        }
    }

    private void SetAdvancedVisible(bool visible)
    {
        _advancedVisible = visible;
        _advancedPanel.Visible = visible;
        _rootLayout.RowStyles[4].Height = visible ? 88 : 0;
        _advancedButton.Text = visible ? "▼  Advanced" : "▶  Advanced";
        ClientSize = new Size(
            ClientSize.Width,
            visible ? ExpandedHeight : CollapsedHeight);
    }

    private sealed record PrinterTypeChoice(
        string Label,
        PrinterType? PrinterType)
    {
        public override string ToString() => Label;
    }
}
