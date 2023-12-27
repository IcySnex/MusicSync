using System.Linq.Expressions;

namespace MusicSync.AndroidMedia.Abstract;

public abstract class TableManager<Record> where Record : Abstract.Record, new()
{
    protected readonly AndroidMediaLibrary library;

    public TableManager(AndroidMediaLibrary library)
    {
        this.library = library;
    }


    public virtual Task<int> CountAsync(Expression<Func<Record, bool>>? predicate = null) =>
        library.CountAsync(predicate);


    public virtual Task<Record[]> GetAllAsync(Expression<Func<Record, bool>>? predicate = null) =>
        library.Get<Record>(predicate).ToArrayAsync();

    public virtual async Task<Record?> GetAsync(
        long id) =>
        await library.Get<Record>(record => record.Id == id).FirstOrDefaultAsync();


    public virtual Task<long> AddAsync(
        Record record,
        bool replace = false) =>
        library.AddAsync(record, replace);


    public virtual Task RemoveAsync(
        long id) =>
        library.RemoveAsync<Record>(record => record.Id == id);

    public virtual Task RemoveAllAsync() =>
        library.RemoveAsync<Record>(null);
}