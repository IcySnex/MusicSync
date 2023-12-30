using MusicSync.AndroidMedia.Manager;
using MusicSync.AndroidMedia.Models;
using iTunesLib;

namespace MusicSync.AndroidMedia;

public static class Helpers
{
    public static async Task<long> GetClearedOrAddPlaylistAsync(
        this PlaylistManager manager,
        Playlist playlist)
    {
        long? localPlaylist = (await manager.GetAsync(playlist.Name))?.Id;
        if (localPlaylist is null)
            localPlaylist = await manager.AddAsync(playlist);
        else
            await manager.RemoveAllTracksFromPlaylistAsync(localPlaylist.Value);

        return localPlaylist.Value;
    }

    public static IITUserPlaylist GetClearedOrAddPlaylist(
        this iTunesApp app,
        string name)
    {
        IITUserPlaylist? iPlaylist = app.LibrarySource.Playlists.OfType<IITUserPlaylist>().FirstOrDefault(pl => pl.Name == name);
        if (iPlaylist is null)
            iPlaylist = (IITUserPlaylist)app.CreatePlaylistInSource(name, app.LibrarySource);
        else
        {
            foreach (IITTrack iTrack in iPlaylist.Tracks)
                iTrack.Delete();
        }

        return iPlaylist;
    }
}