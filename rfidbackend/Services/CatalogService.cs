using Rfid.WebApi.Repositories;

namespace Rfid.WebApi.Services;

public class CatalogService<T> : ICatalogService<T> where T : class
{
    private readonly IRepository<T> _repository;

    public CatalogService(IRepository<T> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _repository.GetAllAsync();

    public async Task<T?> GetByIdAsync(int id) =>
        await _repository.GetByIdAsync(id);

    public async Task<T> CreateAsync(T entity)
    {
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return false;

        _repository.Remove(entity);
        await _repository.SaveChangesAsync();
        return true;
    }
}
