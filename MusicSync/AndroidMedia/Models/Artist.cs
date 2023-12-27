using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("artists")]
public class Artist : Record
{
    public Artist() : base() { }

    public Artist(
        string name,
        string key) : base()
    {
        Name = name;
        Key = key;
    }


    [Column("artist")]
    public string Name { get; set; } = string.Empty;

    [Column("artist_key")]
    public string Key { get; set; } = string.Empty;
}