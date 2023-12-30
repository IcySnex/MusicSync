using MusicSync.AndroidMedia.Abstract;
using MusicSync.AndroidMedia.Models;
using MusicSync.AndroidMedia.Models.Internal;

namespace MusicSync.AndroidMedia.Manager;

public class GenreManager : TableManager<Genre>
{
    public GenreManager(AndroidMediaLibrary library) : base(library) { }


    public async Task<Genre?> GetAsync(
        string name) =>
        await library.Get<Genre>(genre => genre.Name == name).FirstOrDefaultAsync();


    public override async Task RemoveAsync(
        long id)
    {
        await library.RemoveAsync<Genre>(genre => genre.Id == id);
        await library.RemoveAsync<GenreMap>(map => map.GenreId == id);
    }

    public override async Task RemoveAllAsync()
    {
        await library.RemoveAsync<Genre>();
        await library.RemoveAsync<GenreMap>();
    }


    public async Task<long> AddTrackToGenreAsync(
        long genreId,
        long trackId) =>
        await library.AddAsync(new GenreMap
            {
                GenreId = genreId,
                TrackId = trackId,
            });

    public Task RemoveTrackFromGenreAsync(
        long genreId,
        long trackId) =>
        library.RemoveAsync<GenreMap>(map => map.GenreId == genreId && map.TrackId == trackId);

    public Task RemoveAllTracksFromGenreAsync(
        long genreId) =>
        library.RemoveAsync<GenreMap>(map => map.GenreId == genreId);
}