using ElegooLink.Desktop.Core;
using ElegooLink.Events;
using System.Text.RegularExpressions;

namespace ElegooLink.Desktop;

internal sealed partial class AutomationSettingsDialog : Form
{
    private List<EventActionRule> _rules = [];
    private bool _loadingEditor;

    public AutomationSettingsDialog() {
        InitializeComponent();
    }

    public AutomationSettingsDialog(IEnumerable<EventActionRule> rules)
        : this() {
        _rules = AutomationCatalog.NormalizeRules(rules);
        PopulateRules();
    }

    public IReadOnlyList<EventActionRule> Rules =>
        _rules.Select(rule => rule.Snapshot()).ToArray();

    private void PopulateRules() {
        _rulesGrid.Rows.Clear();
        foreach (var rule in _rules) {
            var rowIndex = _rulesGrid.Rows.Add(
                rule.Enabled,
                FriendlyEventName(rule.EventKind),
                rule.ExecutablePath,
                rule.RunHidden);
            _rulesGrid.Rows[rowIndex].Tag = rule;
        }

        if (_rulesGrid.Rows.Count > 0) {
            _rulesGrid.Rows[0].Selected = true;
            _rulesGrid.CurrentCell = _rulesGrid.Rows[0].Cells[1];
        }
    }

    private void RulesGrid_SelectionChanged(object? sender, EventArgs eventArgs) =>
        LoadSelectedRule();

    private void RulesGrid_CurrentCellDirtyStateChanged(
        object? sender,
        EventArgs eventArgs) {
        if (_rulesGrid.IsCurrentCellDirty) {
            _rulesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void EditorTextBox_TextChanged(object? sender, EventArgs eventArgs) =>
        UpdateSelectedRuleFromEditor();

    private void LoadSelectedRule() {
        if (SelectedRule() is not { } rule) {
            return;
        }

        _loadingEditor = true;
        try {
            _executableTextBox.Text = rule.ExecutablePath;
            _argumentsTextBox.Text = rule.ArgumentsTemplate;
            _workingDirectoryTextBox.Text = rule.WorkingDirectory;
        } finally {
            _loadingEditor = false;
        }
    }

    private void UpdateSelectedRuleFromEditor() {
        if (_loadingEditor || SelectedRule() is not { } rule) {
            return;
        }

        rule.ExecutablePath = _executableTextBox.Text.Trim();
        rule.ArgumentsTemplate = _argumentsTextBox.Text;
        rule.WorkingDirectory = _workingDirectoryTextBox.Text.Trim();
        _rulesGrid.SelectedRows[0].Cells[2].Value = rule.ExecutablePath;
        if (string.IsNullOrWhiteSpace(rule.ExecutablePath) == false) _rulesGrid.SelectedRows[0].Cells[0].Value = true;
    }

    private void RulesGrid_CellValueChanged(
        object? sender,
        DataGridViewCellEventArgs eventArgs) {
        if (eventArgs.RowIndex < 0 ||
            _rulesGrid.Rows[eventArgs.RowIndex].Tag is not EventActionRule rule) {
            return;
        }

        if (eventArgs.ColumnIndex == 0) {
            rule.Enabled = Convert.ToBoolean(
                _rulesGrid.Rows[eventArgs.RowIndex].Cells[0].Value);
        } else if (eventArgs.ColumnIndex == 3) {
            rule.RunHidden = Convert.ToBoolean(
                _rulesGrid.Rows[eventArgs.RowIndex].Cells[3].Value);
        }
    }

    private void BrowseExecutable_Click(object? sender, EventArgs eventArgs) {
        using var dialog = new OpenFileDialog {
            CheckFileExists = true,
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Choose an executable"
        };
        if (File.Exists(_executableTextBox.Text)) {
            dialog.FileName = _executableTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK) {
            _executableTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_workingDirectoryTextBox.Text)) {
                _workingDirectoryTextBox.Text =
                    Path.GetDirectoryName(dialog.FileName) ?? "";
            }
        }
    }

    private void BrowseDirectory_Click(object? sender, EventArgs eventArgs) {
        using var dialog = new FolderBrowserDialog {
            Description = "Choose the working directory",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_workingDirectoryTextBox.Text)
                ? _workingDirectoryTextBox.Text
                : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) {
            _workingDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs eventArgs) {
        _rulesGrid.EndEdit();
        UpdateSelectedRuleFromEditor();

        foreach (var rule in _rules) {
            var errors = EventActionRuleValidator.Validate(rule);
            if (errors.Count == 0) {
                continue;
            }

            var row = _rulesGrid.Rows
                .Cast<DataGridViewRow>()
                .First(item => ReferenceEquals(item.Tag, rule));
            row.Selected = true;
            _rulesGrid.CurrentCell = row.Cells[1];
            MessageBox.Show(
                this,
                $"{FriendlyEventName(rule.EventKind)}:{Environment.NewLine}" +
                string.Join(
                    Environment.NewLine,
                    errors.Select(error => $"• {error}")),
                "Invalid event action",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private EventActionRule? SelectedRule() =>
        _rulesGrid.SelectedRows.Count == 0
            ? null
            : _rulesGrid.SelectedRows[0].Tag as EventActionRule;

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex EventWordBoundaryRegex();

    private static string FriendlyEventName(PrinterEventKind eventKind) =>
        EventWordBoundaryRegex().Replace(eventKind.ToString(), " $1");
}
