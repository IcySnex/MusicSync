using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("albums")]
public class Album : Record
{
    public Album() : base() { }

    public Album(
        string name,
        string key,
        string artLocation) : base()
    {
        Name = name;
        Key = key;
        ArtLocation = artLocation;
    }


    [Column("album")]
    public string Name { get; set; } = string.Empty;

    [Column("album_key")]
    public string Key { get; set; } = string.Empty;

    [Column("_data")]
    public string ArtLocation { get; set; } = string.Empty;

    [PrimaryKey, AutoIncrement, Column("album_id")]
    public override long Id { get; set; }
}