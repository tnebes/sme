using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class NativeMethods
{
    // P/Invoke declarations for necessary WinAPI functions
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}

public class MapTool
{
    private static Form mainForm;
    private static string mapFilePath;

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        CreateAndShowForm();
        Application.Run(mainForm);
    }

    private static void CreateAndShowForm()
    {
        mainForm = new Form();
        mainForm.Text = "Stronghold Map Tool";
        mainForm.Size = new Size(400, 300);

        var openFileButton = new Button
        {
            Text = "Open Map",
            Location = new Point(10, 10),
            AutoSize = true
        };
        openFileButton.Click += OpenFileButton_Click;

        var unlockButton = new Button
        {
            Text = "Unlock Map",
            Location = new Point(10, 50),
            AutoSize = true
        };
        unlockButton.Click += UnlockButton_Click;

        var invasionButton = new Button
        {
            Text = "Make Invasion Map",
            Location = new Point(10, 90),
            AutoSize = true
        };
        invasionButton.Click += InvasionButton_Click;

        var siegeButton = new Button
        {
            Text = "Make Siege Map",
            Location = new Point(10, 130),
            AutoSize = true
        };
        siegeButton.Click += SiegeButton_Click;

        mainForm.Controls.Add(openFileButton);
        mainForm.Controls.Add(unlockButton);
        mainForm.Controls.Add(invasionButton);
        mainForm.Controls.Add(siegeButton);
    }

    private static void OpenFileButton_Click(object sender, EventArgs e)
    {
        using (var openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "Map Files|*.map";
            openFileDialog.Title = "Choose the Map to open";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                mapFilePath = openFileDialog.FileName;
                MessageBox.Show($"Selected map: {Path.GetFileName(mapFilePath)}", "Map Selected");
            }
        }
    }

    private static void UnlockButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(mapFilePath))
        {
            MessageBox.Show("Please open a map file first.", "Error");
            return;
        }

        UnlockOrChangeMap(0x3c, new byte[] { 0x01, 0x00 });
    }

    private static void InvasionButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(mapFilePath))
        {
            MessageBox.Show("Please open a map file first.", "Error");
            return;
        }

        UnlockOrChangeMap(0x3c, new byte[] { 0x00, 0x00 });
    }

    private static void SiegeButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(mapFilePath))
        {
            MessageBox.Show("Please open a map file first.", "Error");
            return;
        }

        UnlockOrChangeMap(0x3c, new byte[] { 0x01, 0x00 });
    }

    private static void UnlockOrChangeMap(int offset, byte[] value)
    {
        if (File.Exists(mapFilePath))
        {
            try
            {
                using (var stream = new FileStream(mapFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Seek to the specific offset in the file
                    stream.Seek(offset, SeekOrigin.Begin);
                    stream.Write(value, 0, value.Length);
                }
                MessageBox.Show("Map updated successfully!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
        else
        {
            MessageBox.Show("Selected file not found.", "Error");
        }
    }
}