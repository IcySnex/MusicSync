using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("audio_playlists")]
public class Playlist : Record
{
    public Playlist() : base() { }

    public Playlist(
        string name,
        long addedAtTimestamp) : base()
    {
        Name = name;
        AddedAtTimestamp = addedAtTimestamp;
    }


    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("date_added")]
    public long AddedAtTimestamp { get; set; }
}