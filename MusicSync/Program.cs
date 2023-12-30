using AdvancedSharpAdbClient;
using iTunesLib;
using MusicSync.AndroidMedia;
using MusicSync.AndroidMedia.Models;
using Newtonsoft.Json.Bson;
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
            Console.WriteLine("[2]  Sync iTunes");
            Console.WriteLine("[3]  Sync Music Files\n");
            Console.WriteLine("[4]  Connect to ADB device");
            Console.WriteLine("[5]  Upload file to device");
            Console.WriteLine("[6]  Download file from device\n");
            Console.WriteLine("[7]  Parse iTunes Library");
            Console.WriteLine("[8]  Parse Android Media Library\n");
            Console.WriteLine("[C]  Display Configuration");
            Console.WriteLine("[E]  Edit Configuration\n");
            Console.WriteLine("[X]  Exit\n\n");

            string? choice = ConsoleHelpers.GetResponse("Press [1] - [8] to continue", "Invalid choice. Please enter a number between [1] and [8].");

            if (choice is null) continue;

            switch (choice.ToLower())
            {
                case "1":
                    await SyncMusicLibraryAsync();
                    break;
                case "2":
                    await SyncITunesAsync();
                    break;
                case "3":
                    await SyncMusicFilesAsync();
                    break;
                case "4":
                    await ConnectToAdbDeviceAsync();
                    break;
                case "5":
                    await UploadFileToDeviceAsync();
                    break;
                case "6":
                    await DownloadFileFromDeviceAsync();
                    break;
                case "7":
                    ParseITunesLibrary();
                    break;
                case "8":
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
            Console.WriteLine("[5]  Change ADB Executable\n");
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

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            Console.Write($"\rDownloading Android Media Library: [{percent}%]"));
        IProgress<int> uploadProcess = new Progress<int>(percent =>
            Console.Write($"\rUploading Android Media Library: [{percent}%]"));

        string currentDatabaseLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "external.db");
        string androidDatabaseLocation = "/data/data/com.android.providers.media/databases/external.db";

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"Connected to ADB device: {info}.\n");


            // Refresh database
            Console.WriteLine($"Refreshing Android Media Library...\n");

            await ADB.RefreshMediaLibraryAsync(device);
            await Task.Delay(3000);


            // Download database
            await ADB.DownloadFileAsync(device, androidDatabaseLocation, currentDatabaseLocation, downloadProcess);
            Console.WriteLine("\n");


            // Sync database to iTunes
            Console.WriteLine($"Syncing library to iTunes...\n");

            iTunesApp iTunes = new();
            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            foreach (IITUserPlaylist playlist in iTunes.LibrarySource.Playlists.OfType<IITUserPlaylist>().Where(playlist => playlist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone && !playlist.Smart))
            {
                long localPlaylist = await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new(playlist.Name, null, DateTime.Now.ToUnixEpoch()));
                foreach (IITTrack track in  playlist.Tracks)
                {
                    if (await library.TrackManager.GetAsync(track.Name, track.Artist) is Track localTrack)
                        await library.PlaylistManager.AddTrackToPlaylistAsync(localPlaylist, localTrack.Id);
                }
            }

            (long, int)[] ratedPlaylistData = new[]
            {
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★★★", "/mnt/sdcard/playlistImage/ratedFive", DateTime.Now.ToUnixEpoch())), 100),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★★☆", "/mnt/sdcard/playlistImage/ratedFour", DateTime.Now.ToUnixEpoch())), 80),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★☆☆", "/mnt/sdcard/playlistImage/ratedThree", DateTime.Now.ToUnixEpoch())), 60),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★☆☆☆", "/mnt/sdcard/playlistImage/ratedTwo", DateTime.Now.ToUnixEpoch())), 40),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★☆☆☆☆", "/mnt/sdcard/playlistImage/ratedOne", DateTime.Now.ToUnixEpoch())), 20)
            };
            foreach (IITTrack track in iTunes.LibraryPlaylist.Tracks.OfType<IITTrack>())
            {
                long ratedPlaylist = ratedPlaylistData.FirstOrDefault(playlist => playlist.Item2 <= track.Rating).Item1;

                if (await library.TrackManager.GetAsync(track.Name, track.Artist) is Track localRatedTrack)
                    await library.PlaylistManager.AddTrackToPlaylistAsync(ratedPlaylist, localRatedTrack.Id);
            }

            await library.UnloadDatabaseAsync();


            // Upload database
            await ADB.UploadFileAsync(device, currentDatabaseLocation, androidDatabaseLocation, null, uploadProcess);
            await ADB.SetPermissionAsync(device, androidDatabaseLocation, 777);
            Console.WriteLine("\n");

            File.Delete(currentDatabaseLocation);
            await Task.Delay(3000);


            // Apply database changes
            Console.WriteLine($"Applying library changes to device...\n");

            await ADB.RefreshMediaLibraryAsync(device);


            ConsoleHelpers.Write($"\nMusic library synchronized successfully.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nSynchronizing Music Library failed (Exception: {ex.Message}).");
        }
    }

    static async Task SyncITunesAsync()
    {
        ConsoleHelpers.WriteClear("[1]  Sync iTunes:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            Console.Write($"\rDownloading Android Media Library: [{percent}%]"));
        
        string currentDatabaseLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "external.db");
        string androidDatabaseLocation = "/data/data/com.android.providers.media/databases/external.db";

        try
        {
            // Connect to device
            (DeviceData device, string info) = await ADB.SelectDeviceAsync();
            Console.WriteLine($"Connected to ADB device: {info}.\n");


            // Refresh database
            Console.WriteLine($"Refreshing Android Media Library...\n");

            await ADB.RefreshMediaLibraryAsync(device);
            await Task.Delay(3000);


            // Download database
            await ADB.DownloadFileAsync(device, androidDatabaseLocation, currentDatabaseLocation, downloadProcess);
            Console.WriteLine("\n");


            // Sync database to iTunes
            Console.WriteLine($"Syncing iTunes to library...\n");

            iTunesApp iTunes = new();
            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            foreach (Playlist playlist in await library.PlaylistManager.GetAllAsync(playlist => playlist.Name != "Quick list" && playlist.Name != "Reorder playlist" && playlist.Name != "Tracks" && playlist.Name != "★★★★★" && playlist.Name != "★★★★☆" && playlist.Name != "★★★☆☆" && playlist.Name != "★★☆☆☆" && playlist.Name != "★☆☆☆☆"))
            {
                IITUserPlaylist iPlaylist = iTunes.GetClearedOrAddPlaylist(playlist.Name);
                foreach (Track track in await library.PlaylistManager.GetAllTracksFromPlaylistAsync(playlist.Id))
                {
                    foreach (IITTrack iTrack in iTunes.LibraryPlaylist.Tracks)
                    {
                        if (iTrack.Name.Contains(track.Name) && track.Artist is not null && iTrack.Artist.Contains(track.Artist))
                            iPlaylist.AddTrack(iTrack);
                    }
                }
            }

            foreach (IITTrack track in iTunes.LibraryPlaylist.Tracks.OfType<IITTrack>())
                track.Rating = 0;

            async Task SyncRatesAsync(
                string playlistName,
                int rating)
            {
                long? ratedPlaylist = (await library.PlaylistManager.GetAsync(playlistName))?.Id;
                if (ratedPlaylist is not null)
                {
                    foreach (Track track in await library.PlaylistManager.GetAllTracksFromPlaylistAsync(ratedPlaylist.Value))
                    {
                        foreach (IITTrack iTrack in iTunes.LibraryPlaylist.Tracks)
                        {
                            if (iTrack.Name.Contains(track.Name) && track.Artist is not null && iTrack.Artist.Contains(track.Artist))
                                iTrack.Rating = rating;
                        }
                    }
                }
            }

            await SyncRatesAsync("★★★★★", 100);
            await SyncRatesAsync("★★★★☆", 80);
            await SyncRatesAsync("★★★☆☆", 60);
            await SyncRatesAsync("★★☆☆☆", 40);
            await SyncRatesAsync("★☆☆☆☆", 20);

            await library.UnloadDatabaseAsync();

            File.Delete(currentDatabaseLocation);


            ConsoleHelpers.Write($"\niTunes synchronized successfully.");
        }
        catch (Exception ex)
        {
            // Failed
            ConsoleHelpers.Write($"\nSynchronizing iTunes failed (Exception: {ex.Message}).");
        }
    }

    static async Task SyncMusicFilesAsync()
    {
        ConsoleHelpers.WriteClear("[3]  Sync Music Files:\n");

        // Validation
        if (!Config.ValidateSyncConfig(config)) return;
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            Console.Write($"\rDownloading Android Media Library: [{percent}%]"));
        IProgress<int> uploadProcess = new Progress<int>(percent =>
            Console.Write($"\rUploading Android Media Library: [{percent}%]"));

        string currentDatabaseLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "external.db");
        string androidDatabaseLocation = "/data/data/com.android.providers.media/databases/external.db";

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


            // Refresh database
            Console.WriteLine($"\nRefreshing Android Media Library...\n");

            await ADB.RefreshMediaLibraryAsync(device);
            await Task.Delay(3000);


            // Download database
            await ADB.DownloadFileAsync(device, androidDatabaseLocation, currentDatabaseLocation, downloadProcess);
            Console.WriteLine("\n");


            // Add Tracks
            Console.WriteLine($"Adding tracks to playlist...\n");

            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            long tracksPlaylist = await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("Tracks", "/mnt/sdcard/playlistImage/tracks", DateTime.Now.ToUnixEpoch()));
            foreach (Track track in await library.TrackManager.GetAllAsync())
                await library.PlaylistManager.AddTrackToPlaylistAsync(tracksPlaylist, track.Id);

            await library.UnloadDatabaseAsync();


            // Upload database
            await ADB.UploadFileAsync(device, currentDatabaseLocation, androidDatabaseLocation, null, uploadProcess);
            await ADB.SetPermissionAsync(device, androidDatabaseLocation, 777);
            Console.WriteLine("\n");

            File.Delete(currentDatabaseLocation);
            await Task.Delay(3000);


            // Apply database changes
            Console.WriteLine($"Applying library changes to device...\n");

            await ADB.RefreshMediaLibraryAsync(device);


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
        ConsoleHelpers.WriteClear("[4]  Connect to ADB device:\n");

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
        ConsoleHelpers.WriteClear("[5]  Upload file to device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> progress = new Progress<int>(percent =>
            Console.Write($"\rUploading file: [{percent}%]"));

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
        ConsoleHelpers.WriteClear("[6]  Download file from device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> progress = new Progress<int>(percent =>
            Console.Write($"\rDownloading file: [{percent}%]"));

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
        ConsoleHelpers.WriteClear("[7]  Parse iTunes Library:\n");

        try
        {
            iTunesApp iTunes = new();

            IEnumerable<IITUserPlaylist> playlists = iTunes.LibrarySource.Playlists.OfType<IITUserPlaylist>().Where(playlist => playlist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone);
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
        ConsoleHelpers.WriteClear("[8]  Parse Android Media Library:\n");

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