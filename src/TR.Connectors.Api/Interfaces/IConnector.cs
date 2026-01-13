using TR.Connectors.Api.Entities;

namespace TR.Connectors.Api.Interfaces;
public interface IConnector
{
    public ILogger Logger { get; set; }
    Task StartUp(string connectionString);
    Task CreateUser(UserToCreate user);
    IEnumerable<Property> GetAllProperties();
    IAsyncEnumerable<UserProperty> GetUserProperties(string userLogin);
    Task<bool> IsUserExists(string userLogin);
    Task UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin);
    IAsyncEnumerable<Permission> GetAllPermissions();
    Task AddUserPermissions(string userLogin, IEnumerable<string> rightIds);
    Task RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds);
    IAsyncEnumerable<string> GetUserPermissions(string userLogin);
}
