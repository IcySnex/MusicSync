using SQLite;

namespace MusicSync.AndroidMedia.Abstract;

public abstract class Record
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public virtual long Id { get; set; }
}