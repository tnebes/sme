using System;
using System.IO;

public class MapTool
{
    private static string mapFilePath;

    public static void Main(string[] args)
    {
        Console.WriteLine("Stronghold Map Tool");
        Console.WriteLine("-------------------");

        if (args.Length > 0 && File.Exists(args[0]))
        {
            mapFilePath = args[0];
            Console.WriteLine($"Using map file from argument: {mapFilePath}");
        }
        else
        {
            while (true)
            {
                Console.Write("Enter the path to your .map file: ");
                mapFilePath = Console.ReadLine();
                if (File.Exists(mapFilePath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("File not found. Please try again.");
                }
            }
        }

        Console.WriteLine();
        DisplayMenu();

        bool running = true;
        while (running)
        {
            Console.Write("> ");
            string choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    UnlockOrChangeMap(0x3c, new byte[] { 0x01, 0x00 }, "Unlock Map");
                    break;
                case "2":
                    UnlockOrChangeMap(0x3c, new byte[] { 0x00, 0x00 }, "Make Invasion Map");
                    break;
                case "3":
                    UnlockOrChangeMap(0x3c, new byte[] { 0x01, 0x00 }, "Make Siege Map");
                    break;
                case "4":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select an option from the menu.");
                    break;
            }
        }

        Console.WriteLine("Exiting Map Tool.");
    }

    private static void DisplayMenu()
    {
        Console.WriteLine("Choose an action:");
        Console.WriteLine("  1. Unlock Map (allow editing in editor)");
        Console.WriteLine("  2. Make Invasion Map");
        Console.WriteLine("  3. Make Siege Map");
        Console.WriteLine("  4. Exit");
    }

    private static void UnlockOrChangeMap(int offset, byte[] value, string actionName)
    {
        try
        {
            using (var stream = new FileStream(mapFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(value, 0, value.Length);
            }
            Console.WriteLine($"Success! '{actionName}' applied to {Path.GetFileName(mapFilePath)}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying '{actionName}': {ex.Message}");
        }
    }
}
