#region

using System;
using System.IO;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Serilog;

#endregion

namespace SME;

public enum MapAction
{
    Unlock,
    MakeInvasion,
    MakeSiege
}

public static class Program
{
    public static ILogger Log { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("sme.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application starting...");

        try
        {
            new Application(Platform.Detect).Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly.");
        }
        finally
        {
            Log.Information("Application shutting down.");
        }
    }
}

public sealed class MainForm : Form
{
    private const string Author = "tnebes";
    private static readonly int CurrentYear = DateTime.Now.Year;
    private readonly Label _selectedFileLabel;
    private readonly Label _statusLabel;
    private string _mapFilePath;

    public MainForm()
    {
        this.Title = "Stronghold Map Editor";
        this.ClientSize = new Size(450, 200);
        this.Resizable = false;

        Button openFileButton = new() { Text = "Open Map File" };
        openFileButton.Click += (s, e) => this.OpenFilePicker();

        this._selectedFileLabel = new Label();
        this._statusLabel = new Label { Text = "Select a map file to begin." };

        Button unlockButton = new() { Text = "Unlock Map" };
        unlockButton.Click += (s, e) => this.UnlockOrChangeMap(MapAction.Unlock);

        Button invasionButton = new() { Text = "Make Invasion Map" };
        invasionButton.Click += (s, e) => this.UnlockOrChangeMap(MapAction.MakeInvasion);

        Button siegeButton = new() { Text = "Make Siege Map" };
        siegeButton.Click += (s, e) => this.UnlockOrChangeMap(MapAction.MakeSiege);

        GroupBox actionsGroupBox = new() { Text = "Map Actions" };
        actionsGroupBox.Content = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5,
            Items = { unlockButton, invasionButton, siegeButton }
        };

        Label footerLabel = new()
        {
            Text = $"By {Author}, {CurrentYear}",
            TextColor = Colors.Gray,
            TextAlignment = TextAlignment.Center
        };

        StackLayout centerLayout = new()
        {
            Spacing = 10,
            Items = { actionsGroupBox, this._statusLabel }
        };

        TableLayout mainLayout = new()
        {
            Padding = new Padding(15),
            Spacing = new Size(10, 10),
            Rows =
            {
                new TableRow(new StackLayout(
                    new StackLayoutItem(openFileButton),
                    new StackLayoutItem(this._selectedFileLabel) { VerticalAlignment = VerticalAlignment.Center }
                ) { Orientation = Orientation.Horizontal, Spacing = 10 }),
                new TableRow(centerLayout) { ScaleHeight = true },
                new TableRow(footerLabel)
            }
        };

        this.Content = mainLayout;
    }

    private void OpenFilePicker()
    {
        OpenFileDialog openDialog = new()
        {
            Title = "Open Map File",
            Filters = { new FileFilter("Stronghold Map", ".map") }
        };

        if (openDialog.ShowDialog(this) == DialogResult.Ok)
        {
            this._mapFilePath = openDialog.FileName;
            this._selectedFileLabel.Text = Path.GetFileName(this._mapFilePath);
            this._statusLabel.Text = $"Selected: {Path.GetFileName(this._mapFilePath)}";
            Program.Log.Information("User selected map file: {FilePath}", this._mapFilePath);
        }
    }

    private void UnlockOrChangeMap(MapAction action)
    {
        /*
         * IMPORTANT: The logic for calculating the write offset is complex and was derived
         * from reverse-engineering an original 2001 executable. It is not a fixed offset.
         * The calculation involves reading two separate pointer values from within the map
         * file itself to dynamically find the correct address for the game type flag.
         *
         * The formula is, in essence:
         *   val1 = Read 2 bytes at offset 0x04
         *   offset_b = val1 + 8
         *   val2 = Read 2 bytes at offset offset_b
         *   final_offset = offset_b + 0x3c + val2
         *
         * An empirical, off-by-one error was discovered during testing, requiring a -1
         * adjustment for the "Unlock Map" action. The reason for this discrepancy is
         * unclear from the disassembly but is necessary for the correct behavior.
         * The "Change Type" actions (Invasion/Siege) require a different adjustment (-4).
         */

        if (string.IsNullOrEmpty(this._mapFilePath) || !File.Exists(this._mapFilePath))
        {
            Program.Log.Warning("UnlockOrChangeMap called but no file was selected.");
            MessageBox.Show(this, "Please select a valid map file first.", "Error", MessageBoxButtons.OK,
                MessageBoxType.Error);
            return;
        }

        string actionName = action switch
        {
            MapAction.Unlock => "Unlock Map",
            MapAction.MakeInvasion => "Make Invasion Map",
            MapAction.MakeSiege => "Make Siege Map",
            _ => "Unknown Action"
        };

        Program.Log.Information("Attempting action '{Action}' on file {File}", actionName, this._mapFilePath);
        this._statusLabel.Text = $"Attempting action '{actionName}'...";

        try
        {
            byte[] valueToWrite = action switch
            {
                MapAction.Unlock => [0x00, 0x00],
                MapAction.MakeInvasion => [0x00, 0x00],
                MapAction.MakeSiege => [0x01, 0x00],
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };

            using FileStream stream = new(this._mapFilePath, FileMode.Open, FileAccess.ReadWrite);
            using BinaryReader reader = new(stream);
            using BinaryWriter writer = new(stream);
            stream.Seek(4, SeekOrigin.Begin);
            ushort val1 = reader.ReadUInt16();

            long offsetB = val1 + 8;
            stream.Seek(offsetB, SeekOrigin.Begin);
            ushort val2 = reader.ReadUInt16();

            long finalOffsetBase = offsetB + 0x3c + val2;

            long writeOffset = action == MapAction.Unlock ? finalOffsetBase - 1 : finalOffsetBase - 4;

            Program.Log.Debug("Calculated values: val1={Val1:X4}, val2={Val2:X4}, write_offset={WriteOffset:X}",
                val1, val2, writeOffset);

            stream.Seek(writeOffset, SeekOrigin.Begin);
            writer.Write(valueToWrite);

            string successMsg = $"Success! '{actionName}' applied to {Path.GetFileName(this._mapFilePath)}.";
            this._statusLabel.Text = successMsg;
            Program.Log.Information(successMsg);
            MessageBox.Show(
                this,
                $"Map updated successfully!\n(Wrote {valueToWrite.Length} bytes to offset {writeOffset:X})",
                "Success", MessageBoxButtons.OK);
        }
        catch (Exception ex)
        {
            this._statusLabel.Text = $"Error: {ex.Message}";
            Program.Log.Error(ex, "An error occurred while trying to modify map file for action '{Action}'",
                actionName);
            MessageBox.Show(this, "An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK,
                MessageBoxType.Error);
        }
    }
}