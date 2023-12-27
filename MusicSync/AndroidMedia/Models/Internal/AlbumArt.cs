using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models.Internal;

[Table("album_art")]
public class AlbumArt : Record
{
    public AlbumArt() : base() { }


    [Column("album_id")]
    public long AlbumId { get; set; } = 0;

    [Column("_data")]
    public string Location { get; set; } = string.Empty;
}