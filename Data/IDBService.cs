namespace WebApplication1.Data;

public interface IDBService
{
    public Task<bool> CheckDBConnection();

    public Task<bool> AddNewCred(object model);

    public Task<object> GetConnectionCredsAsync(object connectionType);
    
}