using MusicSync.AndroidMedia.Abstract;
using MusicSync.AndroidMedia.Models;
using MusicSync.AndroidMedia.Models.Internal;
using System.Linq.Expressions;

namespace MusicSync.AndroidMedia.Manager;

public class AlbumManager : TableManager<Album>
{
    public AlbumManager(AndroidMediaLibrary library) : base(library) { }


    public override async Task<Album[]> GetAllAsync(
        Expression<Func<Album, bool>>? predicate = null)
    {
        List<Album> albums = await library.QueryAsync<Album>("SELECT albums.album_id, albums.album, albums.album_key, album_art._data FROM albums JOIN album_art ON albums.album_id = album_art.album_id");
        if (predicate is null)
            return albums.ToArray();

        return albums.Where(predicate.Compile()).ToArray();
    }

    public override async Task<Album?> GetAsync(
        long id) =>
        (await library.QueryAsync<Album>($"SELECT albums.album_id, albums.album, albums.album_key, album_art._data FROM albums JOIN album_art ON albums.album_id = album_art.album_id WHERE albums.album_id = {id}")).FirstOrDefault();

    public async Task<Album?> GetAsync(
        string name) =>
        (await library.QueryAsync<Album>($"SELECT albums.album_id, albums.album, albums.album_key, album_art._data FROM albums JOIN album_art ON albums.album_id = album_art.album_id WHERE albums.album = '{name}'")).FirstOrDefault();


    public override async Task<long> AddAsync(
        Album record,
        bool replace = false)
    {
        if (replace)
            await library.ExecuteAsync($"INSERT INTO albums (album_id, album, album_key) VALUES ({record.Id}, '{record.Name}', '{record.Key}')");
        else
            await library.ExecuteAsync($"INSERT INTO albums (album, album_key) VALUES ('{record.Name}', '{record.Key}')");
        long id = await library.GetLastInsertedIdASync();

        if (record.ArtLocation is not null)
            await library.AddAsync(new AlbumArt
                {
                    AlbumId = id,
                    Location = record.ArtLocation
                });

        return id;

    }


    public override async Task RemoveAsync(
        long id)
    {
        Album? album = await GetAsync(id);
        if (album is null)
            return;

        Track[] albumTracks = await library.TrackManager.GetAllAsync(track => track.AlbumId == album.Id);
        foreach (Track track in albumTracks)
        {
            track.AlbumId = null;
            await library.TrackManager.AddAsync(track, true);
        }

        await library.RemoveAsync<Album>(album => album.Id == id);
        await library.RemoveAsync<AlbumArt>(art => art.AlbumId == id);
    }

    public override async Task RemoveAllAsync()
    {
        Track[] tracks = await library.TrackManager.GetAllAsync();
        foreach (Track track in tracks)
        {
            track.AlbumId = null;
            await library.TrackManager.AddAsync(track, true);
        }

        await library.RemoveAsync<Album>();
        await library.RemoveAsync<AlbumArt>();
    }
}