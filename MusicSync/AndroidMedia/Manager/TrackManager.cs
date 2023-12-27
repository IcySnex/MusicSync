using MusicSync.AndroidMedia.Abstract;
using MusicSync.AndroidMedia.Models;
using MusicSync.AndroidMedia.Models.Internal;
using System.Text;
using File = MusicSync.AndroidMedia.Models.Internal.File;

namespace MusicSync.AndroidMedia.Manager;

public class TrackManager : TableManager<Track>
{
    public TrackManager(AndroidMediaLibrary library) : base(library) { }


    public async Task<Track?> GetAsync(
        string name) =>
        await library.Get<Track>(record => record.Name == name).FirstOrDefaultAsync();


    public override async Task<long> AddAsync(
        Track record,
        bool replace = false)
    {
        File file = new()
        {
            Data = record.Location,
            Size = record.Size,
            Format = 12297,
            //Parent = 502,
            DateAdded = record.AddedAtTimestamp,
            DateModified = record.ModifedAtTimestamp,
            MimeType = "audio/mpeg",
            Title = record.Name,
            TitleKey = AndroidMediaLibrary.CreateKey(record.Name),
            DisplayName = Path.GetFileName(record.Location),
            //BucketId = 1940887661,
            //BucketDisplayName = "Music",
            ArtistId = record.ArtistId,
            AlbumId = record.AlbumId,
            //Track = xxxx,
            IsMusic = true,
            Duration = record.DurationInMs,
            MediaType = 2,
            //StorageId = 131073,
            GenreName = record.Genre
        };
        if (replace)
            file.Id = record.Id;
        long id = await library.AddAsync(file, replace);

        Genre? genre = await library.GenreManager.GetAsync(record.Genre);
        if (genre is null)
        {
            genre = new(record.Genre);
            genre.Id = await library.GenreManager.AddAsync(genre, replace);
        }
        await library.GenreManager.AddTrackToGenreAsync(genre.Id, id);

        return id;
    }


    public override async Task RemoveAsync(
        long id)
    {
        Track? track = await GetAsync(id);
        if (track is null)
            return;

        await library.RemoveAsync<File>(file => file.Id == track.Id);
        await library.RemoveAsync<PlaylistMap>(map => map.TrackId == track.Id);
        await library.RemoveAsync<GenreMap>(map => map.TrackId == track.Id);

        if (await library.TrackManager.CountAsync(track => track.ArtistId == track.ArtistId) == 0 && track.ArtistId.HasValue)
            await library.ArtistManager.RemoveAsync(track.ArtistId.Value);

        if (await library.TrackManager.CountAsync(track => track.AlbumId == track.AlbumId) == 0 && track.AlbumId.HasValue)
            await library.AlbumManager.RemoveAsync(track.AlbumId.Value);
    }

    public override async Task RemoveAllAsync()
    {
        await library.RemoveAsync<File>(file => file.MediaType == 2);
        await library.RemoveAsync<PlaylistMap>();
        await library.RemoveAsync<GenreMap>();
        await library.AlbumManager.RemoveAllAsync();
        await library.ArtistManager.RemoveAllAsync();
    }
}