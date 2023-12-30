using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("audio")]
public class Track : Record
{
    public Track() : base() { }

    public Track(
        string name,
        long? artistId,
        long? albumId,
        string genre,
        string location,
        int size,
        int durationInMs,
        long addedAtTimestamp,
        long modifedAtTimestamp) : base()
    {
        Name = name;
        ArtistId = artistId;
        AlbumId = albumId;
        Genre = genre;
        Location = location;
        Size = size;
        DurationInMs = durationInMs;
        AddedAtTimestamp = addedAtTimestamp;
        ModifedAtTimestamp = modifedAtTimestamp;
    }


    [Column("title")]
    public string Name { get; set; } = string.Empty;

    [Column("artist_id")]
    public long? ArtistId { get; set; } = null;
    
    [Column("artist")]
    public string? Artist { get; set; } = null;

    [Column("album_id")]
    public long? AlbumId { get; set; } = null;

    [Column("album")]
    public string? Album { get; set; } = null;

    [Column("genre_name")]
    public string Genre { get; set; } = string.Empty;

    [Column("_data")]
    public string Location { get; set; } = string.Empty;

    [Column("_size")]
    public int Size { get; set; } = 0;

    [Column("duration")]
    public int DurationInMs { get; set; } = 0;

    [Column("date_added")]
    public long AddedAtTimestamp { get; set; } = 0;

    [Column("date_modified")]
    public long ModifedAtTimestamp { get; set; } = 0;
}