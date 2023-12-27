using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models;

[Table("audio_genres")]
public class Genre : Record
{
    public Genre() : base() { }

    public Genre(
        string name) : base()
    {
        Name = name;
    }


    [Column("name")]
    public string Name { get; set; } = string.Empty;
}