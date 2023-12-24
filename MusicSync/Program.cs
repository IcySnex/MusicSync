using AdvancedSharpAdbClient;
using ITunesLibraryParser;
using System.ComponentModel;
using System.Reflection.Emit;

namespace MusicSync;

public class Program
{
    static Config config = new();

    public static async Task Main()
    {
        config = Config.Load("config.json");

        while (true)
        {
            ConsoleHelpers.WriteClear("Music Sync:\n");

            Console.WriteLine("[1]  Sync Music\n");
            Console.WriteLine("[2]  Connect to ADB device");
            Console.WriteLine("[3]  Upload file to device");
            Console.WriteLine("[4]  Parse iTunes Library\n");
            Console.WriteLine("[C]  Display Configuration");
            Console.WriteLine("[E]  Edit Configuration\n");
            Console.WriteLine("[X]  Exit\n\n");

            string? choice = ConsoleHelpers.GetResponse("Press [1] - [4] to continue", "Invalid choice. Please enter a number between [1] and [4].");

            if (choice is null) continue;

            switch (choice.ToLower())
            {
                case "1":
                    await SyncMusicAsync();
                    break;
                case "2":
                    await ConnectToAdbDeviceAsync();
                    break;
                case "3":
                    await UploadFileToDeviceAsync();
                    break;
                case "4":
                    ParseITunesLibrary();
                    break;
                case "c":
                    DisplayConfiguration(config);
                    break;
                case "e":
                    EditConfiguration(config);
                    break;
                case "x":
                    Exit();
                    return;
            }
        }
    }


    static void DisplayConfiguration(Config config)
    {
        ConsoleHelpers.WriteClear("[C]  Display Current Configuration:\n");

        Console.WriteLine($"Sync From Location: {config.SyncFromLocation}");
        Console.WriteLine($"Sync To Location: {config.SyncToLocation}");
        Console.WriteLine($"Sync Max Count: {config.SyncMaxCount}");
        Console.WriteLine($"Overwrite Already Synced: {config.OverwriteAlreadySyncred}");
        Console.WriteLine($"ADB Executable: {config.AdbExecutable}");
        Console.WriteLine($"ITunes Library Xml: {config.ITunesLibraryXml}");

        ConsoleHelpers.Write($"\n");
    }

    static void EditConfiguration(Config config)
    {
        while (true)
        {
            ConsoleHelpers.WriteClear("[E]  Edit Configuration:\n");

            Console.WriteLine("[1]  Change Sync From Location");
            Console.WriteLine("[2]  Change Sync To Location");
            Console.WriteLine("[3]  Change Sync Max Count");
            Console.WriteLine("[4]  Change Overwrite Already Synced");
            Console.WriteLine("[5]  Change ADB Executable");
            Console.WriteLine("[6]  Change ITunes Library Xml\n");
            Console.WriteLine("[X]  Exit Configuration Update\n\n");

            string? choice = ConsoleHelpers.GetResponse("Press [1] - [5] to continue", "Invalid choice. Please enter a number between [1] and [5].");

            if (choice is null) continue;

            Console.Clear();
            switch (choice.ToLower())
            {
                case "1":
                    config.SyncFromLocation = ConsoleHelpers.GetResponse("Enter new Sync From Location", null) ?? string.Empty;
                    Console.WriteLine("Sync From Location updated.");
                    break;
                case "2":
                    config.SyncToLocation = ConsoleHelpers.GetResponse("Enter new Sync To Location", null) ?? string.Empty;
                    Console.WriteLine("Sync To Location updated.");
                    break;
                case "3":
                    if (int.TryParse(ConsoleHelpers.GetResponse("Enter new Sync Max Count", null), out int maxCount))
                        config.SyncMaxCount = maxCount;
                    Console.WriteLine("Sync Max Count updated.");
                    break;
                case "4":
                    config.OverwriteAlreadySyncred = ConsoleHelpers.AskResponse("Do you want to overwrite already synced? (Y/N)");
                    Console.WriteLine("Overwrite Already Synced updated.");
                    break;
                case "5":
                    config.AdbExecutable = ConsoleHelpers.GetResponse("Enter new ADB Executable", null) ?? string.Empty;
                    Console.WriteLine("ADB Executable updated.");
                    break;
                case "6":
                    config.ITunesLibraryXml = ConsoleHelpers.GetResponse("Enter new ITunes Library Xml", null) ?? string.Empty;
                    Console.WriteLine("ITunes Library Xml updated.");
                    break;
                case "x":
                    config.Save("config.json");
                    ConsoleHelpers.Write("\nConfiguration updated.");
                    return;
            }
        }
    }


    static void Exit()
    {
        ConsoleHelpers.WriteClear("[X]  Exit:");
        Environment.Exit(0);
    }


    static async Task SyncMusicAsync()
    {
        ConsoleHelpers.WriteClear("[S]  Sync Music:\n");

        // Validation
        if (!Config.ValidateSyncConfig(config)) return;
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.Wrapper.StartServerASync(config.AdbExecutable)) return;

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.Wrapper.SelectDeviceAsync();
            Console.WriteLine($"Connected to ADB device: {info}.\n");

            // Files
            FileInfo[] files = new DirectoryInfo(config.SyncFromLocation)
                .GetFiles()
                .OrderBy(file => file.LastWriteTime)
                .Take(config.SyncMaxCount)
                .ToArray();
            string[] existingFiles = ADB.Wrapper.GetFiles(device, config.SyncToLocation);

            for (int i = 0; i < files.Length; i++)
            {
                // Sync file
                FileInfo file = files[i];
                if (existingFiles.Contains(file.Name) && !config.OverwriteAlreadySyncred)
                {
                    Console.WriteLine($"[{i + 1}/{files.Length}]  Skipping '{file.Name}'");
                    continue;
                }

                IProgress<int> progress = new Progress<int>(percent =>
                    Console.Write($"\r[{i + 1}/{files.Length}]  Synchronizing '{file.Name}': [{percent}%]"));

                await ADB.Wrapper.UploadFileAsync(device, file.FullName, Path.Combine(config.SyncToLocation, file.Name).Replace('\\', '/'), file.LastWriteTime, progress);
                Console.WriteLine();
            }
            ConsoleHelpers.Write($"\nMusic synchronized successfully.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nSynchronizing music failed (Exception: {ex.Message}).");
        }
    }


    static async Task ConnectToAdbDeviceAsync()
    {
        ConsoleHelpers.WriteClear("[2]  Connect to ADB device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.Wrapper.StartServerASync(config.AdbExecutable)) return;

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.Wrapper.SelectDeviceAsync();
            ConsoleHelpers.Write($"Connected to ADB device: {info}.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nConnecting to ADB device failed (Exception: {ex.Message}).");
        }
    }


    static async Task UploadFileToDeviceAsync()
    {
        ConsoleHelpers.WriteClear("[3]  Upload file to device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.Wrapper.StartServerASync(config.AdbExecutable)) return;

        // Get file paths
        string? filePath = ConsoleHelpers.GetResponse("Enter the path to the file you want to upload to the device", "File path can not be empty.");
        if (filePath is null) return;
        
        string? saveToPath = ConsoleHelpers.GetResponse("Enter the location where the file should be saved", "File path can not be empty.");
        if (saveToPath is null) return;


        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.Wrapper.SelectDeviceAsync();
            Console.WriteLine($"\nConnected to ADB device: {info}.\n");

            // Sync file
            IProgress<int> progress = new Progress<int>(percent =>
                Console.Write($"\rUploading file: [{percent}%]"));

            await ADB.Wrapper.UploadFileAsync(device, filePath, saveToPath, null, progress);
            ConsoleHelpers.Write($"\nUploaded file to device.\n");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nUploading file failed (Exception: {ex.Message}).");
        }
    }


    static void ParseITunesLibrary()
    {
        ConsoleHelpers.WriteClear("[4]  Parse iTunes Library:\n");

        // Validation
        if (!Config.ValidateITunesConfig(config)) return;

        try
        {
            ITunesLibrary library = new(config.ITunesLibraryXml);

            Console.WriteLine($"Track count: {library.Tracks.Count()}");
            Console.WriteLine($"Album count: {library.Albums.Count()}");
            Console.WriteLine($"Playlist count: {library.Playlists.Where(playlist => playlist.Tracks.Any() && playlist.Name != "Downloaded" && playlist.Name != "Library" && playlist.Name != "Music").Count()}");

            var s = library.Tracks.Select(track => track.Loved);

            ConsoleHelpers.Write($"\nParsed iTunes Library.");

        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nParsing iTunes Library failed (Exception: {ex.Message}).");
        }
    }
}