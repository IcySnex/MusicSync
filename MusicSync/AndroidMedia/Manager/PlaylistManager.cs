using MusicSync.AndroidMedia.Abstract;
using MusicSync.AndroidMedia.Models;
using MusicSync.AndroidMedia.Models.Internal;
using SQLite;
using File = MusicSync.AndroidMedia.Models.Internal.File;

namespace MusicSync.AndroidMedia.Manager;

public class PlaylistManager : TableManager<Playlist>
{
    public PlaylistManager(AndroidMediaLibrary library) : base(library) { }


    public async Task<Playlist?> GetAsync(
        string name) =>
        await library.Get<Playlist>(playlist => playlist.Name == name).FirstOrDefaultAsync();


    public override Task<long> AddAsync(
        Playlist record,
        bool replace = false)
    {
        File file = new()
        {
            Data = $"/mnt/sdcard/Playlists/{record.Name}",
            Format = 47621,
            //Parent = 440,
            MiniThumbData = record.ArtLocation,
            DateAdded = record.AddedAtTimestamp,
            Name = record.Name,
            MediaType = 4,
            //StorageId = 65537
        };
        if (replace)
            file.Id = record.Id;
        return library.AddAsync(file, replace);
    }


    public override async Task RemoveAsync(
        long id)
    {
        await library.RemoveAsync<File>(file => file.Id == id);
        await library.RemoveAsync<PlaylistMap>(map => map.PlaylistId == id);
    }

    public override async Task RemoveAllAsync()
    {
        await library.RemoveAsync<File>(file => file.MediaType == 4 && file.Name != "Quick list");
        await library.RemoveAsync<PlaylistMap>();
    }


    public async Task<Track[]> GetAllTracksFromPlaylistAsync(
        long playlistId)
    {
        AsyncTableQuery<PlaylistMap> maps = library.Get<PlaylistMap>(map => map.PlaylistId == playlistId);

        List<Track> tracks = new();
        foreach (PlaylistMap map in await maps.ToArrayAsync())
        {
            if (await library.TrackManager.GetAsync(map.TrackId) is Track track)
                tracks.Add(track);
        }
        
        return tracks.ToArray();
    }

    public async Task<long> AddTrackToPlaylistAsync(
        long playlistId,
        long trackId)
    {
        PlaylistMap? lastMap = await library.Get<PlaylistMap>(map => map.PlaylistId == playlistId).OrderByDescending(map => map.PlayOrder).FirstOrDefaultAsync();
        return await library.AddAsync(new PlaylistMap
        {
            PlaylistId = playlistId,
            TrackId = trackId,
            PlayOrder = lastMap?.PlayOrder + 1 ?? 0,
        });
    }

    public Task RemoveTrackFromPlaylistAsync(
        long playlistId,
        long trackId) =>
        library.RemoveAsync<PlaylistMap>(map => map.PlaylistId == playlistId && map.TrackId == trackId);

    public Task RemoveAllTracksFromPlaylistAsync(
        long playlistId) =>
        library.RemoveAsync<PlaylistMap>(map => map.PlaylistId == playlistId);
}