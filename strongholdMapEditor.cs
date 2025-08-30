#region

using System;
using System.IO;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Serilog;

#endregion

public class Program
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
    private readonly Label _selectedFileLabel;
    private readonly Label _statusLabel;
    private string _mapFilePath;

    public MainForm()
    {
        this.Title = "Stronghold Map Editor";
        this.ClientSize = new Size(450, 150);
        this.Resizable = false;

        Button openFileButton = new() { Text = "Open Map File" };
        openFileButton.Click += (s, e) => this.OpenFilePicker();

        this._selectedFileLabel = new Label();
        this._statusLabel = new Label { Text = "Select a map file to begin." };

        Button unlockButton = new() { Text = "Unlock Map" };
        unlockButton.Click += (s, e) => this.UnlockOrChangeMap("Unlock Map", new byte[] { 0x01, 0x00 });

        Button invasionButton = new() { Text = "Make Invasion Map" };
        invasionButton.Click += (s, e) => this.UnlockOrChangeMap("Make Invasion Map", new byte[] { 0x00, 0x00 });

        Button siegeButton = new() { Text = "Make Siege Map" };
        siegeButton.Click += (s, e) => this.UnlockOrChangeMap("Make Siege Map", new byte[] { 0x01, 0x00 });

        this.Content = new StackLayout
        {
            Padding = new Padding(10),
            Spacing = 10,
            Items =
            {
                new StackLayout(
                    new StackLayoutItem(openFileButton, HorizontalAlignment.Left),
                    new StackLayoutItem(this._selectedFileLabel, HorizontalAlignment.Left)
                ) { Orientation = Orientation.Horizontal, Spacing = 10 },
                new TableLayout
                {
                    Spacing = new Size(5, 5),
                    Rows = { new TableRow(unlockButton, invasionButton, siegeButton) }
                },
                this._statusLabel
            }
        };
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

    private void UnlockOrChangeMap(string action, byte[] valueToWrite)
    {
        if (string.IsNullOrEmpty(this._mapFilePath) || !File.Exists(this._mapFilePath))
        {
            Program.Log.Warning("UnlockOrChangeMap called but no file was selected.");
            MessageBox.Show(this, "Please select a valid map file first.", "Error", MessageBoxButtons.OK,
                MessageBoxType.Error);
            return;
        }

        Program.Log.Information("Attempting action '{Action}' on file {File}", action, this._mapFilePath);
        this._statusLabel.Text = $"Attempting action '{action}'...";

        try
        {
            using FileStream stream = new(this._mapFilePath, FileMode.Open, FileAccess.ReadWrite);
            using BinaryReader reader = new(stream);
            using BinaryWriter writer = new(stream);
            stream.Seek(4, SeekOrigin.Begin);
            ushort val1 = reader.ReadUInt16();

            long offsetB = val1 + 8;
            stream.Seek(offsetB, SeekOrigin.Begin);
            ushort val2 = reader.ReadUInt16();

            long finalOffsetBase = offsetB + 0x3c + val2;

            long writeOffset;
            if (action == "Unlock Map")
            {
                writeOffset = finalOffsetBase - 1;
            }
            else
            {
                writeOffset = finalOffsetBase - 4;
            }

            Program.Log.Debug("Calculated values: val1={Val1:X4}, val2={Val2:X4}, write_offset={WriteOffset:X}",
                val1, val2, writeOffset);

            stream.Seek(writeOffset, SeekOrigin.Begin);
            writer.Write(valueToWrite);

            string successMsg = $"Success! '{action}' applied to {Path.GetFileName(this._mapFilePath)}.";
            this._statusLabel.Text = successMsg;
            Program.Log.Information(successMsg);
            MessageBox.Show(this,
                "Map updated successfully!\n(Wrote {valueToWrite.Length} bytes to offset {write_offset:X})",
                "Success", MessageBoxButtons.OK);
        }
        catch (Exception ex)
        {
            this._statusLabel.Text = $"Error: {ex.Message}";
            Program.Log.Error(ex, "An error occurred while trying to modify map file for action '{Action}'", action);
            MessageBox.Show(this, "An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK,
                MessageBoxType.Error);
        }
    }
}