using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models.Internal;

[Table("audio_genres_map")]
class GenreMap : Record
{
    public GenreMap() : base() { }


    [Column("genre_id")]
    public long GenreId { get; set; } = 0;

    [Column("audio_id")]
    public long TrackId { get; set; } = 0;
}