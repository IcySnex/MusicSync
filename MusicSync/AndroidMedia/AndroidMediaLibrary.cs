using MusicSync.AndroidMedia.Manager;
using MusicSync.AndroidMedia.Models;
using MusicSync.AndroidMedia.Models.Internal;
using SQLite;
using System.Linq.Expressions;
using System.Text;

namespace MusicSync.AndroidMedia;

public class AndroidMediaLibrary
{
    class Trigger
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("tbl_name")]
        public string TableName { get; set; } = string.Empty;

        [Column("sql")]
        public string Sql { get; set; } = string.Empty;
    }


    public static string CreateKey(
        string name) =>
        Encoding.ASCII.GetString(BitConverter.GetBytes(name.GetHashCode()));


    SQLiteAsyncConnection connection = default!;

    List<Trigger> triggers = default!;


    public bool IsLoaded => connection != null;

    public static readonly SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.SharedCache;

    public TrackManager TrackManager { get; private set; } = default!;
    public PlaylistManager PlaylistManager { get; private set; } = default!;
    public ArtistManager ArtistManager { get; private set; } = default!;
    public AlbumManager AlbumManager { get; private set; } = default!;
    public GenreManager GenreManager { get; private set; } = default!;


    public async Task LoadDatabaseAsync(
        string path)
    {
        connection = new(path, Flags);

        await connection.CreateTableAsync<Models.Internal.File>();
        await connection.CreateTableAsync<Playlist>();
        await connection.CreateTableAsync<PlaylistMap>();

        triggers = await connection.QueryAsync<Trigger>("SELECT name, tbl_name, sql FROM sqlite_master WHERE type='trigger'");
        foreach (Trigger trigger in triggers)
            await connection.ExecuteAsync($"DROP TRIGGER IF EXISTS {trigger.Name}");

        TrackManager = new(this);
        PlaylistManager = new(this);
        ArtistManager = new(this);
        AlbumManager = new(this);
        GenreManager = new(this);
    }

    public async Task UnloadDatabaseAsync()
    {
        foreach (Trigger trigger in triggers)
            await connection.ExecuteAsync(trigger.Sql);

        await connection.CloseAsync();
        connection = default!;

        TrackManager = default!;
        PlaylistManager = default!;
        ArtistManager = default!;
        AlbumManager = default!;
        GenreManager = default!;
    }


    public Task<long> GetLastInsertedIdASync()
    {
        if (!IsLoaded)
            throw new Exception("Database is not loaded. Please first run 'LoadDatabaseAsync()'.");

        return connection.ExecuteScalarAsync<long>($"SELECT last_insert_rowid()");
    }

    public Task<int> CountAsync<T>(
        Expression<Func<T, bool>>? predicate = null) where T : new()
    {
        if (!IsLoaded)
            throw new Exception("Database is not loaded. Please first run 'LoadDatabaseAsync()'.");

        return predicate is null ? connection.Table<T>().CountAsync() : connection.Table<T>().Where(predicate).CountAsync();
    }

    public Task<List<T>> QueryAsync<T>(
        string sql) where T : new() =>
        connection.QueryAsync<T>(sql);

    public Task<int> ExecuteAsync(
        string sql) =>
        connection.ExecuteAsync(sql);


    public AsyncTableQuery<T> Get<T>(
        Expression<Func<T, bool>>? predicate = null) where T : new()
    {
        if (!IsLoaded)
            throw new Exception("Database is not loaded. Please first run 'LoadDatabaseAsync()'.");

        return predicate is null ? connection.Table<T>() : connection.Table<T>().Where(predicate);
    }


    public async Task<long> AddAsync(
        object item,
        bool replace = false)
    {
        if (!IsLoaded)
            throw new Exception("Database is not loaded. Please first run 'LoadDatabaseAsync()'.");

        await (replace ? connection.InsertOrReplaceAsync(item) : connection.InsertAsync(item));

        return await GetLastInsertedIdASync();
    }


    public Task RemoveAsync<T>(
        Expression<Func<T, bool>>? predicate = null) where T : new()
    {
        if (!IsLoaded)
            throw new Exception("Database is not loaded. Please first run 'LoadDatabaseAsync()'.");

        return predicate is null ? connection.DeleteAllAsync<T>() : connection.Table<T>().DeleteAsync(predicate); ;
    }
}