using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models.Internal;

[Table("audio_playlists_map")]
class PlaylistMap : Record
{
    public PlaylistMap() : base() { }


    [Column("playlist_id")]
    public long PlaylistId { get; set; } = 0;

    [Column("audio_id")]
    public long TrackId { get; set; } = 0;

    [Column("play_order")]
    public int PlayOrder { get; set; } = 0;
}