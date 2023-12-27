using MusicSync.AndroidMedia.Abstract;
using SQLite;

namespace MusicSync.AndroidMedia.Models.Internal;

[Table("files")]
public class File : Record
{
    public File() : base() { }


    [Column("_data")]
    public string? Data { get; set; }
    
    [Column("_size")]
    public int? Size { get; set; }

    [Column("format")]
    public int? Format { get; set; }

    [Column("parent")]
    public int? Parent { get; set; }

    [Column("date_added")]
    public long? DateAdded { get; set; }

    [Column("date_modified")]
    public long? DateModified { get; set; }

    [Column("mime_type")]
    public string? MimeType { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("title_key")]
    public string? TitleKey { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("_display_name")]
    public string? DisplayName { get; set; }

    [Column("genre_name")]
    public string? GenreName { get; set; }

    [Column("year_name")]
    public string? YearName { get; set; }

    [Column("picasa_id")]
    public string? PicasaId { get; set; }

    [Column("bucket_id")]
    public int? BucketId { get; set; }

    [Column("bucket_display_name")]
    public int? BucketDisplayName { get; set; }

    [Column("artist_id")]
    public long? ArtistId { get; set; }

    [Column("album_id")]
    public long? AlbumId { get; set; }

    [Column("album_artist")]
    public string? AlbumArtist { get; set; }

    [Column("duration")]
    public long? Duration { get; set; }

    [Column("orientation")]
    public int? Orientation { get; set; }

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("datetaken")]
    public int? DateTaken { get; set; }

    [Column("mini_thumb_magic")]
    public int? MiniThumbMagic { get; set; }

    [Column("isprivate")]
    public int? IsPrivate { get; set; }

    [Column("year")]
    public int? Year { get; set; }

    [Column("is_drm")]
    public bool IsDrm { get; set; } = false;

    [Column("is_music")]
    public bool? IsMusic { get; set; }

    [Column("is_ringtone")]
    public bool? IsRingtone { get; set; }

    [Column("is_alarm")]
    public bool? IsAlarm { get; set; }

    [Column("is_notification")]
    public bool? IsNotification { get; set; }

    [Column("is_podcast")]
    public bool? IsPodcast { get; set; }

    [Column("bookmark")]
    public int? Bookmark { get; set; }

    [Column("artist")]
    public string? Artist { get; set; }

    [Column("album")]
    public string? Album { get; set; }

    [Column("resolution")]
    public string? Resolution { get; set; }

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("language")]
    public string? Language { get; set; }

    [Column("mini_thumb_data")]
    public string? MiniThumbData { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("media_type")]
    public int? MediaType { get; set; }

    [Column("storage_id")]
    public int? StorageId { get; set; }
}