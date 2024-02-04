using AdvancedSharpAdbClient;
using iTunesLib;
using MusicSync.AndroidMedia;
using MusicSync.AndroidMedia.Models;
using System.Diagnostics;
using System.Linq;

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

            Console.WriteLine("[1]  Sync Android Media Library to iTunes");
            Console.WriteLine("[2]  Sync iTunes to Android Media Library\n");
            Console.WriteLine("[3]  Sync Music Files to Android");
            Console.WriteLine("[4]  Sync Music Files to iTunes\n");
            Console.WriteLine("[5]  Connect to ADB device");
            Console.WriteLine("[6]  Upload file to device");
            Console.WriteLine("[7]  Download file from device\n");
            Console.WriteLine("[8]  Parse iTunes Library");
            Console.WriteLine("[9]  Parse Android Media Library\n");
            Console.WriteLine("[C]  Display Configuration");
            Console.WriteLine("[E]  Edit Configuration\n");
            Console.WriteLine("[X]  Exit\n\n");

            string? choice = ConsoleHelpers.GetResponse("Press [1] - [9] to continue", "Invalid choice. Please enter a number between [1] and [9].");

            if (choice is null) continue;

            switch (choice.ToLower())
            {
                case "1":
                    await SyncAndroidMediaLibraryAsync();
                    break;
                case "2":
                    await SyncITunesAsync();
                    break;
                case "3":
                    await SyncMusicFilesToAndroidAsync();
                    break;
                case "4":
                    SyncMusicFilesToITunes();
                    break;
                case "5":
                    await ConnectToAdbDeviceAsync();
                    break;
                case "6":
                    await UploadFileToDeviceAsync();
                    break;
                case "7":
                    await DownloadFileFromDeviceAsync();
                    break;
                case "8":
                    ParseITunesLibrary();
                    break;
                case "9":
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


    static void DisplayConfiguration(
        Config config)
    {
        ConsoleHelpers.WriteClear("[C]  Display Current Configuration:\n");

        Console.WriteLine($"Sync From Location: {config.SyncFromLocation}");
        Console.WriteLine($"Sync To Location: {config.SyncToLocation}");
        Console.WriteLine($"Sync Max Count: {config.SyncMaxCount}");
        Console.WriteLine($"Overwrite Already Synced: {config.OverwriteAlreadySyncred}");
        Console.WriteLine($"ADB Executable: {config.AdbExecutable}");

        ConsoleHelpers.Write($"\n");
    }

    static void EditConfiguration(
        Config config)
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


    static async Task SyncAndroidMediaLibraryAsync()
    {
        ConsoleHelpers.WriteClear("[1]  Sync Android Media Library to iTunes:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Downloading Android Media Library: [{percent}%]"));
        IProgress<int> uploadProcess = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Uploading Android Media Library: [{percent}%]"));

        string currentDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "external.db");
        string backupDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "backups", "Android Media Library", $"external [{DateTime.Now:yyyy-MM-dd-HH-mm}].db");
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

            if (!Directory.Exists(Path.GetDirectoryName(backupDatabaseLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(backupDatabaseLocation)!);
            File.Copy(currentDatabaseLocation, backupDatabaseLocation, true);


            // Sync database to iTunes
            Console.WriteLine($"Syncing library to iTunes...\n");
            iTunesApp iTunes = new();
            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            Console.WriteLine($"Preparing synchronization...\n");
            IITFileOrCDTrack[] tracks = iTunes.LibraryPlaylist.Tracks.OfType<IITFileOrCDTrack>().Where(track => track.Rating >= 20).ToArray();
            Track[] localTracks = await library.TrackManager.GetAllAsync();

            IITUserPlaylist[] playlists = iTunes.LibrarySource.Playlists.OfType<IITUserPlaylist>().Where(playlist => playlist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone && !playlist.Smart).ToArray();
            (long id, int rating)[] ratedPlaylistData = new[]
            {
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★★★", "/mnt/sdcard/playlistImage/ratedFive", DateTime.Now.ToUnixEpoch())), 100),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★★☆", "/mnt/sdcard/playlistImage/ratedFour", DateTime.Now.ToUnixEpoch())), 80),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★★☆☆", "/mnt/sdcard/playlistImage/ratedThree", DateTime.Now.ToUnixEpoch())), 60),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★★☆☆☆", "/mnt/sdcard/playlistImage/ratedTwo", DateTime.Now.ToUnixEpoch())), 40),
                (await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("★☆☆☆☆", "/mnt/sdcard/playlistImage/ratedOne", DateTime.Now.ToUnixEpoch())), 20)
            };
            long favouritesPlaylist = await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("♥", "/mnt/sdcard/playlistImage/favourites", DateTime.Now.ToUnixEpoch()));

            for (int currentPlaylist = 0; currentPlaylist < playlists.Length; currentPlaylist++)
            {
                ConsoleHelpers.WriteClearLine($"Syncing playlists: [{currentPlaylist + 1}/{playlists.Length}]");
                IITUserPlaylist playlist = playlists[currentPlaylist];

                IITFileOrCDTrack[] playlistTracks = playlist.Tracks.OfType<IITFileOrCDTrack>().ToArray();
                int playlistTrackCount = playlistTracks.Count();

                long localPlaylist = await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new(playlist.Name, null, DateTime.Now.ToUnixEpoch()));

                for (int currentPlaylistTrack = 0; currentPlaylistTrack < playlistTrackCount; currentPlaylistTrack++)
                {
                    ConsoleHelpers.WriteClearLine($"Syncing playlists: [{currentPlaylist + 1}/{playlists.Length}] [{currentPlaylistTrack + 1}/{playlistTrackCount}]");
                    IITFileOrCDTrack track = playlistTracks[currentPlaylistTrack];

                    if (localTracks.FirstOrDefault(local => local.Name == track.Name && local.Artist == track.Artist) is not Track localTrack)
                        continue;

                    await library.PlaylistManager.AddTrackToPlaylistAsync(localPlaylist, localTrack.Id);
                }
            }
            Console.WriteLine("\n");

            for (int currentTrack = 0; currentTrack < tracks.Length; currentTrack++)
            {
                ConsoleHelpers.WriteClearLine($"Syncing ratings and favourites: [{currentTrack + 1}/{tracks.Length}]");
                IITFileOrCDTrack track = tracks[currentTrack];

                if (localTracks.FirstOrDefault(local => local.Name == track.Name && local.Artist == track.Artist) is not Track localTrack)
                    continue;

                await library.PlaylistManager.AddTrackToPlaylistAsync(ratedPlaylistData.FirstOrDefault(playlist => playlist.rating <= track.Rating).id, localTrack.Id);

                if (track.Rating >= 60)
                    await library.PlaylistManager.AddTrackToPlaylistAsync(favouritesPlaylist, localTrack.Id);

            }
            Console.WriteLine("\n");

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
        ConsoleHelpers.WriteClear("[1]  Sync iTunes to Android Media Library:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Downloading Android Media Library: [{percent}%]"));
        
        string currentDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "external.db");
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

            string iTunesDatabaseLocation = Path.Combine(Path.GetDirectoryName(iTunes.LibraryXMLPath)!, "iTunes Library.itl");
            string backupDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "backups", "iTunes", $"iTunes Library [{DateTime.Now:yyyy-MM-dd-HH-mm}].itl");

            if (!Directory.Exists(Path.GetDirectoryName(backupDatabaseLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(backupDatabaseLocation)!);
            File.Copy(iTunesDatabaseLocation, backupDatabaseLocation, true);


            Console.WriteLine($"Preparing synchronization...\n");
            IITFileOrCDTrack[] iTracks = iTunes.LibraryPlaylist.Tracks.OfType<IITFileOrCDTrack>().ToArray();

            Playlist[] playlists = await library.PlaylistManager.GetAllAsync(playlist => playlist.Name != "Quick list" && playlist.Name != "Reorder playlist" && playlist.Name != "♬" && playlist.Name != "♥" && playlist.Name != "★★★★★" && playlist.Name != "★★★★☆" && playlist.Name != "★★★☆☆" && playlist.Name != "★★☆☆☆" && playlist.Name != "★☆☆☆☆");
            (Playlist? playlist, int rating)[] ratedPlaylistData = new[]
            {
                (await library.PlaylistManager.GetAsync("★☆☆☆☆"), 20),
                (await library.PlaylistManager.GetAsync("★★☆☆☆"), 40),
                (await library.PlaylistManager.GetAsync("★★★☆☆"), 60),
                (await library.PlaylistManager.GetAsync("★★★★☆"), 80),
                (await library.PlaylistManager.GetAsync("★★★★★"), 100),
            };

            for (int currentPlaylist = 0; currentPlaylist < playlists.Length; currentPlaylist++)
            {
                ConsoleHelpers.WriteClearLine($"Syncing playlists: [{currentPlaylist + 1}/{playlists.Length}]");
                Playlist playlist = playlists[currentPlaylist];

                Track[] playlistTracks = await library.PlaylistManager.GetAllTracksFromPlaylistAsync(playlist.Id);

                IITUserPlaylist iPlaylist = iTunes.GetClearedOrAddPlaylist(playlist.Name);

                for (int currentPlaylistTrack = 0; currentPlaylistTrack < playlistTracks.Length; currentPlaylistTrack++)
                {
                    ConsoleHelpers.WriteClearLine($"Syncing playlists: [{currentPlaylist + 1}/{playlists.Length}] [{currentPlaylistTrack + 1}/{playlistTracks.Length}]");
                    Track track = playlistTracks[currentPlaylistTrack];

                    if (iTracks.FirstOrDefault(i => i.Name == track.Name && i.Artist == track.Artist) is not IITFileOrCDTrack iTrack)
                        continue;

                    iPlaylist.AddTrack(iTrack);
                }
            }
            Console.WriteLine("\n");

            Console.WriteLine($"Clearing ratings...\n");
            foreach (IITFileOrCDTrack track in iTracks)
                track.Rating = 0;

            for (int currentRatedPlaylist = 0; currentRatedPlaylist < ratedPlaylistData.Length; currentRatedPlaylist++)
            {
                ConsoleHelpers.WriteClearLine($"Syncing ratings: [{currentRatedPlaylist + 1}/{ratedPlaylistData.Length}]");
                (Playlist? playlist, int rating) ratedPlaylist = ratedPlaylistData[currentRatedPlaylist];

                if (ratedPlaylist.playlist is null)
                    continue;

                Track[] ratedPlaylistTracks = await library.PlaylistManager.GetAllTracksFromPlaylistAsync(ratedPlaylist.playlist.Id);

                for (int currentRatedPlaylistTrack = 0; currentRatedPlaylistTrack < ratedPlaylistTracks.Length; currentRatedPlaylistTrack++)
                {
                    ConsoleHelpers.WriteClearLine($"Syncing ratings: [{currentRatedPlaylist + 1}/{ratedPlaylistData.Length}] [{currentRatedPlaylistTrack + 1}/{ratedPlaylistTracks.Length}]");
                    Track track = ratedPlaylistTracks[currentRatedPlaylistTrack];

                    if (iTracks.FirstOrDefault(i => i.Name == track.Name && i.Artist == track.Artist) is not IITFileOrCDTrack iTrack)
                        continue;

                    iTrack.Rating = ratedPlaylist.rating;
                }
            }
            Console.WriteLine("\n");

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


    static async Task SyncMusicFilesToAndroidAsync()
    {
        ConsoleHelpers.WriteClear("[3]  Sync Music Files to Android:\n");

        // Validation
        if (!Config.ValidateSyncConfig(config)) return;
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> downloadProcess = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Downloading Android Media Library: [{percent}%]"));
        IProgress<int> uploadProcess = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Uploading Android Media Library: [{percent}%]"));

        string currentDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "external.db");
        string backupDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "backups", "Android Media Library", $"external [{DateTime.Now:yyyy-MM-dd-HH-mm}].db");
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
                    ConsoleHelpers.WriteClearLine($"[{i + 1}/{files.Length}]  Synchronizing '{file.Name}': [{percent}%]"));

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

            if (!Directory.Exists(Path.GetDirectoryName(backupDatabaseLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(backupDatabaseLocation)!);
            File.Copy(currentDatabaseLocation, backupDatabaseLocation, true);


            // Add Tracks
            Console.WriteLine($"Updating library...\n");
            AndroidMediaLibrary library = new();
            await library.LoadDatabaseAsync(currentDatabaseLocation);

            Console.WriteLine($"Preparing update...\n");
            long tracksPlaylist = await library.PlaylistManager.GetClearedOrAddPlaylistAsync(new("♬", "/mnt/sdcard/playlistImage/tracks", DateTime.Now.ToUnixEpoch()));
            Track[] tracks = await library.TrackManager.GetAllAsync(track => track.Name != "Join Hangout");

            for (int currentTrack = 0; currentTrack < tracks.Length; currentTrack++)
            {
                ConsoleHelpers.WriteClearLine($"Adding tracks to library: [{currentTrack + 1}/{tracks.Length}]");
                Track track = tracks[currentTrack];

                await library.PlaylistManager.AddTrackToPlaylistAsync(tracksPlaylist, track.Id);
            }
            Console.WriteLine("\n");

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
    
    static void SyncMusicFilesToITunes()
    {
        ConsoleHelpers.WriteClear("[4]  Sync Music Files to iTunes:\n");

        try
        {
            // Sync database to iTunes
            Console.WriteLine($"Preparing synchronization...\n");
            iTunesApp iTunes = new();
            IITUserPlaylist playlist = Helpers.GetOrAddPlaylist(iTunes, "♫");
            IITFileOrCDTrack[] iTracks = playlist.Tracks.OfType<IITFileOrCDTrack>().ToArray();
            string[] iFiles = iTracks.Select(i => Path.GetFileName(i.Location)).ToArray();

            string iTunesDatabaseLocation = Path.Combine(Path.GetDirectoryName(iTunes.LibraryXMLPath)!, "iTunes Library.itl");
            string backupDatabaseLocation = Path.Combine(Environment.CurrentDirectory, "backups", "iTunes", $"iTunes Library [{DateTime.Now:yyyy-MM-dd-HH-mm}].itl");

            if (!Directory.Exists(Path.GetDirectoryName(backupDatabaseLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(backupDatabaseLocation)!);
            File.Copy(iTunesDatabaseLocation, backupDatabaseLocation, true);

            // Files
            FileInfo[] files = new DirectoryInfo(config.SyncFromLocation)
                .GetFiles()
                .OrderBy(file => file.LastWriteTime)
                .Take(config.SyncMaxCount)
                .ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                // Sync file
                FileInfo file = files[i];
                if (iFiles.Contains(file.Name))
                {
                    Console.WriteLine($"[{i + 1}/{files.Length}]  Skipping '{file.Name}'");
                    continue;
                }

                ConsoleHelpers.WriteClearLine($"[{i + 1}/{files.Length}]  Synchronizing '{file.Name}'");
                playlist.AddFile(file.FullName);
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
        ConsoleHelpers.WriteClear("[5]  Connect to ADB device:\n");

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
        ConsoleHelpers.WriteClear("[6]  Upload file to device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> progress = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Uploading file: [{percent}%]"));

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
        ConsoleHelpers.WriteClear("[7]  Download file from device:\n");

        // Validation
        if (!Config.ValidateAdbConfig(config)) return;
        if (!await ADB.StartServerASync(config.AdbExecutable)) return;

        IProgress<int> progress = new Progress<int>(percent =>
            ConsoleHelpers.WriteClearLine($"Downloading file: [{percent}%]"));

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
        ConsoleHelpers.WriteClear("[8]  Parse iTunes Library:\n");

        try
        {
            iTunesApp iTunes = new();

            IITUserPlaylist[] playlists = iTunes.LibrarySource.Playlists.OfType<IITUserPlaylist>().Where(playlist => playlist.SpecialKind == ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone).ToArray();
            //IITFileOrCDTrack[] tracks = iTunes.LibraryPlaylist.Tracks.Cast<IITFileOrCDTrack>().ToArray();

            Console.WriteLine($"Track count: {iTunes.LibraryPlaylist.Tracks.Count}");
            Console.WriteLine($"Playlist count: {playlists.Length}");

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