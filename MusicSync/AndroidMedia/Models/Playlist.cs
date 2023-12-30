using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("audio_playlists")]
public class Playlist : Record
{
    public Playlist() : base() { }

    public Playlist(
        string name,
        string? artLocation,
        long addedAtTimestamp) : base()
    {
        Name = name;
        ArtLocation = artLocation;
        AddedAtTimestamp = addedAtTimestamp;
    }


    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("mini_thumb_data")]
    public string? ArtLocation { get; set; } = null;

    [Column("date_added")]
    public long AddedAtTimestamp { get; set; }
}