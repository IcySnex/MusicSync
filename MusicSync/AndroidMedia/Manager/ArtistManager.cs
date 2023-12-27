using MusicSync.AndroidMedia.Abstract;
using MusicSync.AndroidMedia.Models;

namespace MusicSync.AndroidMedia.Manager;

public class ArtistManager : TableManager<Artist>
{
    public ArtistManager(AndroidMediaLibrary library) : base(library) { }


    public async Task<Artist?> GetAsync(
        string name) =>
        await library.Get<Artist>(record => record.Name == name).FirstOrDefaultAsync();


    public override async Task RemoveAsync(
        long id)
    {
        Artist? artist = await GetAsync(id);
        if (artist is null)
            return;

        Track[] artistTracks = await library.TrackManager.GetAllAsync(track => track.ArtistId == artist.Id);
        foreach (Track track in artistTracks)
        {
            track.ArtistId = null;
            await library.TrackManager.AddAsync(track, true);
        }

        await library.RemoveAsync<Artist>(record => record.Id == id);
    }

    public override async Task RemoveAllAsync()
    {
        Track[] tracks = await library.TrackManager.GetAllAsync();
        foreach (Track track in tracks)
        {
            track.ArtistId = null;
            await library.TrackManager.AddAsync(track, true);
        }

        await library.RemoveAsync<Artist>(null);
    }
}