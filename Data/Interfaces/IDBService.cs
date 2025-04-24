namespace WebApplication1.Data.Interfaces;

public interface IDBService
{
    public Task<bool> CheckDBConnection();

    public Task<bool> AddNewCred(object model);

    public Task<object> GetConnectionCredsAsync(object connectionType);

    public Task<bool> Update(object model);
}