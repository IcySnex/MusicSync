using AdvancedSharpAdbClient;
using iTunesLib;
using MusicSync.AndroidMedia;
using MusicSync.AndroidMedia.Models;
using System.Reflection;

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

            Console.WriteLine("[1]  Sync Music Library");
            Console.WriteLine("[2]  Sync Music Files\n");
            Console.WriteLine("[3]  Connect to ADB device");
            Console.WriteLine("[4]  Upload file to device");
            Console.WriteLine("[5]  Download file from device\n");
            Console.WriteLine("[6]  Parse iTunes Library");
            Console.WriteLine("[7]  Parse Android Media Library\n");
            Console.WriteLine("[C]  Display Configuration");
            Console.WriteLine("[E]  Edit Configuration\n");
            Console.WriteLine("[X]  Exit\n\n");

            string? choice = ConsoleHelpers.GetResponse("Press [1] - [6] to continue", "Invalid choice. Please enter a number between [1] and [6].");

            if (choice is null) continue;

            switch (choice.ToLower())
            {
                case "1":
                    await SyncMusicLibraryAsync();
                    break;
                case "2":
                    await SyncMusicFilesAsync();
                    break;
                case "3":
                    await ConnectToAdbDeviceAsync();
                    break;
                case "4":
                    await UploadFileToDeviceAsync();
                    break;
                case "5":
                    await DownloadFileFromDeviceAsync();
                    break;
                case "6":
                    ParseITunesLibrary();
                    break;
                case "7":
                    await ParseAndroidMediaibraryAsync();
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


    static async Task SyncMusicLibraryAsync()
    {
        ConsoleHelpers.WriteClear("[1]  Sync Music Library:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;
        if (!Config.ValidateITunesConfig(config)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            Console.Write($"\rDownloading Android Media Library... [{percent}%]"));
        IProgress<int> uploadProcess = new Progress<int>(percent =>
            Console.Write($"\rUploading Android Media Library... [{percent}%]"));

        string currentDatabaseLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "external.db");
        string androidDatabaseLocation = "/data/data/com.android.providers.media/databases/external.db";

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"Connected to ADB device: {info}.\n");


            // Refresh database
            Console.WriteLine($"Refreshing Android Media Library...\n");

            await ADB.RunCommandAsync(device, "am force-stop com.android.providers.media");
            await ADB.RunCommandAsync(device, "am start -n \"com.sec.android.app.music/.MusicActionTabActivity\"");
            await Task.Delay(3000);


            // Download database
            await ADB.DownloadFileAsync(device, androidDatabaseLocation, currentDatabaseLocation, downloadProcess);
            Console.WriteLine("\n");
            await Task.Delay(3000);


            // Clear database
            Console.WriteLine($"Clearing Android Media Library...\n");

            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            long playlist = await library.PlaylistManager.AddAsync(new Playlist("SSSSSSSSSS", DateTime.Now.ToUnixEpoch()));
            long playlistMap = await library.PlaylistManager.AddTrackToPlaylistAsync(playlist, 242);

            //List<Track> tracks = await library.PlaylistManager.GetAllTracksFromPlaylistAsync(playlist);


            // Sync database to iTunes
            Console.WriteLine($"Syncing Android Media Library to iTunes...\n");

            await library.UnloadDatabaseAsync();
            await Task.Delay(3000);


            // Upload database
            await ADB.UploadFileAsync(device, currentDatabaseLocation, androidDatabaseLocation, null, uploadProcess);
            Console.WriteLine("\n");

            File.Delete(currentDatabaseLocation);
            await Task.Delay(3000);


            // Apply database changes
            Console.WriteLine($"Applying Android Media Library changes to device...\n");

            await ADB.RunCommandAsync(device, "am force-stop com.android.providers.media");
            await ADB.RunCommandAsync(device, "am start -n \"com.sec.android.app.music/.MusicActionTabActivity\"");
            await Task.Delay(3000);


            ConsoleHelpers.Write($"\nMusic Library synchronized successfully.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nSynchronizing Music Library failed (Exception: {ex.Message}).");
        }
    }

    static async Task SyncMusicFilesAsync()
    {
        ConsoleHelpers.WriteClear("[2]  Sync Music Files:\n");

        // Validation
        if (!Config.ValidateSyncConfig(config)) return;
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"Connected to ADB device: {info}.\n");

            // Files
            FileInfo[] files = new DirectoryInfo(config.SyncFromLocation)
                .GetFiles()
                .OrderBy(file => file.LastWriteTime)
                .Take(config.SyncMaxCount)
                .ToArray();
            string[] existingFiles = ADB.GetFiles(device, config.SyncToLocation);

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

                await ADB.UploadFileAsync(device, file.FullName, Path.Combine(config.SyncToLocation, file.Name).Replace('\\', '/'), file.LastWriteTime, progress);
                Console.WriteLine();
            }
            ConsoleHelpers.Write($"\nMusic Files synchronized successfully.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nSynchronizing Music Files failed (Exception: {ex.Message}).");
        }
    }


    static async Task ConnectToAdbDeviceAsync()
    {
        ConsoleHelpers.WriteClear("[3]  Connect to ADB device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
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
        ConsoleHelpers.WriteClear("[4]  Upload file to device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        // Get file paths
        string? filePath = ConsoleHelpers.GetResponse("Enter the path to the file you want to upload to the device", "File path can not be empty.");
        if (filePath is null) return;
        
        string? saveToPath = ConsoleHelpers.GetResponse("Enter the location where the file should be saved", "File path can not be empty.");
        if (saveToPath is null) return;


        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"\nConnected to ADB device: {info}.\n");

            // Sync file
            IProgress<int> progress = new Progress<int>(percent =>
                Console.Write($"\rUploading file: [{percent}%]"));

            await ADB.UploadFileAsync(device, filePath, saveToPath, null, progress);
            ConsoleHelpers.Write($"\nUploaded file to device.\n");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nUploading file failed (Exception: {ex.Message}).");
        }
    }
    
    static async Task DownloadFileFromDeviceAsync()
    {
        ConsoleHelpers.WriteClear("[5]  Download file from device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        // Get file paths
        string? filePath = ConsoleHelpers.GetResponse("Enter the path to the file you want to download from the device", "File path can not be empty.");
        if (filePath is null) return;
        
        string? saveToPath = ConsoleHelpers.GetResponse("Enter the location where the file should be saved", "File path can not be empty.");
        if (saveToPath is null) return;


        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"\nConnected to ADB device: {info}.\n");

            // Sync file
            IProgress<int> progress = new Progress<int>(percent =>
                Console.Write($"\rDownloading file: [{percent}%]"));

            await ADB.DownloadFileAsync(device, filePath, saveToPath, progress);
            ConsoleHelpers.Write($"\nDownloaded file from device.\n");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nDownloading file failed (Exception: {ex.Message}).");
        }
    }


    static void ParseITunesLibrary()
    {
        ConsoleHelpers.WriteClear("[6]  Parse iTunes Library:\n");

        // Validation
        if (!Config.ValidateITunesConfig(config)) return;

        try
        {
            iTunesApp iTunes = new();

            IEnumerable<IITPlaylist> playlists = iTunes.LibrarySource.Playlists.Cast<IITPlaylist>().Where(playlist => 
                playlist.Kind == ITPlaylistKind.ITPlaylistKindUser && playlist.Name != "Music" && playlist.Name != "Movies" && playlist.Name != "TV Shows" && playlist.Name != "Podcasts" &&  playlist.Name != "Audiobooks");
            //IEnumerable<IITTrack> tracks = iTunes.LibraryPlaylist.Tracks.Cast<IITTrack>();

            Console.WriteLine($"Track count: {iTunes.LibraryPlaylist.Tracks.Count}");
            Console.WriteLine($"Playlist count: {playlists.Count()}");

            ConsoleHelpers.Write($"\nParsed iTunes Library.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nParsing iTunes Library failed (Exception: {ex.Message}).");
        }
    }

    static async Task ParseAndroidMediaibraryAsync()
    {
        ConsoleHelpers.WriteClear("[7]  Parse Android Media Library:\n");

        // Get file paths
        string? filePath = ConsoleHelpers.GetResponse("Enter the path to the Android Media external database", "File path can not be empty.");
        if (filePath is null) return;

        try
        {
            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(filePath);

            Console.WriteLine($"\nTrack count: {await library.TrackManager.CountAsync()}");
            Console.WriteLine($"Playlist count: {await library.PlaylistManager.CountAsync()}");
            Console.WriteLine($"Album count: {await library.AlbumManager.CountAsync()}");
            Console.WriteLine($"Artist count: {await library.ArtistManager.CountAsync()}");
            Console.WriteLine($"Genres count: {await library.GenreManager.CountAsync()}");

            await library.UnloadDatabaseAsync();
            ConsoleHelpers.Write($"\nParsed Android Media Library.");

        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nParsing Android Media Library failed (Exception: {ex.Message}).");
        }

    }
}