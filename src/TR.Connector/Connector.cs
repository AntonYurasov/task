using System.Collections;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TR.Connectors.Api.Entities;
using TR.Connectors.Api.Interfaces;

namespace TR.Connector
{
    public enum ERoleType
    {
        ItRole,
        RequestRight
    }

    public partial class Connector : IConnector, IDisposable
    {
        public ILogger Logger { get; set; }

        // private string url = "";
        // private string login = "";
        // private string password = "";

        private string _token = "";

        private HttpClient _httpClient;

        //Пустой конструктор
        public Connector()
        {
        }

        public async Task StartUp(string connectionString)
        {
            string url = "", login = "", password = "";

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception($"Connector::StartUp: Invalid connection string: {connectionString}");

            //Парсим строку подключения.
            Logger.Debug("Строка подключения: " + connectionString);

            foreach (var item in connectionString.Split(';'))
            {
                if (item.StartsWith("url")) url = item.Split('=')[1];
                if (item.StartsWith("login")) login = item.Split('=')[1];
                if (item.StartsWith("password")) password = item.Split('=')[1];
            }

            //Проходим аунтификацию на сервере.
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(url);
            await TryLogin(login, password);
        }

        private async Task TryLogin(string login, string password)
        {
            try
            {
                var body = new { login, password };
                var content = new StringContent(JsonSerializer.Serialize(body), UnicodeEncoding.UTF8,
                    "application/json");
                var response = await _httpClient.PostAsync("api/v1/login", content);
                var tokenResponse =
                    await JsonSerializer.DeserializeAsync<TokenResponse>(await response.Content.ReadAsStreamAsync());
                _token = tokenResponse.data.access_token;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::TryLogin: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public async IAsyncEnumerable<Permission> GetAllPermissions()
        {
            IEnumerable<Permission>[] permissionsArray;

            //Получаем ИТРоли
            try
            {
                var rolesRequest = GetPermissionsForRole(ERoleType.ItRole);
                var rightsRequest = GetPermissionsForRole(ERoleType.RequestRight);

                permissionsArray = await Task.WhenAll(rolesRequest, rightsRequest);

                if (!rolesRequest.IsCompletedSuccessfully || !rightsRequest.IsCompletedSuccessfully)
                    throw new Exception($"Invalid response from server for one of the requests.");
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetAllPermissions: {e.Message}\n{e.StackTrace}");
                throw;
            }

            foreach (var permissions in permissionsArray)
            {
                foreach (var permission in permissions)
                {
                    yield return permission;
                }
            }
        }

        private async Task<IEnumerable<Permission>> GetPermissionsForRole(ERoleType roleType)
        {
            RoleResponse? roleResponse = null;

            try
            {
                switch (roleType)
                {
                    case ERoleType.ItRole:
                        roleResponse = await GetAsync<RoleResponse>("api/v1/roles/all");
                        break;
                    case ERoleType.RequestRight:
                        roleResponse = await GetAsync<RoleResponse>("api/v1/rights/all");
                        break;
                }

                if (roleResponse == null)
                    throw new Exception($"Response is null from server for {roleType}!");

                return roleResponse.data.Select(x => x.FormPermission(roleType));
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetPermissionsForRole: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public async IAsyncEnumerable<string> GetUserPermissions(string userLogin)
        {
            IEnumerable<string>[] permissionsArray;

            try
            {
                //Получаем ИТРоли
                var rolesRequest = GetUserPermissionsForRole(userLogin, ERoleType.ItRole);
                //Получаем права
                var rightsRequest = GetUserPermissionsForRole(userLogin, ERoleType.RequestRight);

                permissionsArray = await Task.WhenAll(rolesRequest, rightsRequest);

                if (!rolesRequest.IsCompletedSuccessfully || !rightsRequest.IsCompletedSuccessfully)
                    throw new Exception($"Invalid response from server for one of the requests.");
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetUserPermissions: {e.Message}\n{e.StackTrace}");
                throw;
            }

            foreach (var permissions in permissionsArray)
            {
                foreach (var permission in permissions)
                {
                    yield return permission;
                }
            }
        }

        private async Task<IEnumerable<string>> GetUserPermissionsForRole(string userLogin, ERoleType roleType)
        {
            UserRoleResponse? roleResponse = null;

            try
            {
                switch (roleType)
                {
                    case ERoleType.ItRole:
                        roleResponse = await GetAsync<UserRoleResponse>($"api/v1/users/{userLogin}/roles");
                        break;
                    case ERoleType.RequestRight:
                        roleResponse = await GetAsync<UserRoleResponse>($"api/v1/users/{userLogin}/rights");
                        break;
                }

                if (roleResponse == null)
                    throw new Exception($"Response is null from server for {roleType}!");

                return roleResponse.data.Select(x => x.FormStringPermission(roleType));
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetUserPermissionsForRole: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public async Task AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                //проверяем что пользователь не залочен.
                var userResponse = await GetAsync<UserResponse>($"api/v1/users/all");
                var user = userResponse.data.FirstOrDefault(_ => _.login == userLogin);

                if (user == null)
                    throw new Exception($"Null user for login {userLogin}!");
                
                if (user.status == "Lock")
                    throw new Exception($"Пользователь {userLogin} залочен.");
                
                //Назначаем права.
                if (user.status == "Unlock")
                {
                    foreach (var rightId in rightIds)
                    {
                        var rightStr = rightId.Split(',');
                        switch (rightStr[0])
                        {
                            case "ItRole":
                                await PutAsync<BaseResponse>($"api/v1/users/{userLogin}/add/role/{rightStr[1]}", null);
                                break;
                            case "RequestRight":
                                await PutAsync<BaseResponse>($"api/v1/users/{userLogin}/add/right/{rightStr[1]}", null);
                                break;
                            default:
                                throw new Exception($"Тип доступа {rightStr[0]} не определен");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::AddUserPermissions: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public async Task RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                //проверяем что пользователь не залочен.
                var userResponse = await GetAsync<UserResponse>($"api/v1/users/all");
                var user = userResponse.data.FirstOrDefault(_ => _.login == userLogin);

                if (user == null)
                    throw new Exception($"Null user for login {userLogin}!");
                
                if (user.status == "Lock")
                    throw new Exception($"Пользователь {userLogin} залочен.");
                
                //отзываем права.
                if (user.status == "Unlock")
                {
                    foreach (var rightId in rightIds)
                    {
                        var rightStr = rightId.Split(',');
                        switch (rightStr[0])
                        {
                            case "ItRole":
                                await DeleteAsync($"api/v1/users/{userLogin}/drop/role/{rightStr[1]}");
                                break;
                            case "RequestRight":
                                await DeleteAsync($"api/v1/users/{userLogin}/drop/right/{rightStr[1]}");
                                break;
                            default:
                                throw new Exception($"Тип доступа {rightStr[0]} не определен");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::RemoveUserPermissions: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public IEnumerable<Property> GetAllProperties()
        {
            return new UserPropertyData().FormProperties();
        }

        public async IAsyncEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            IEnumerable<UserProperty> userProperties;

            try
            {
                var userResponse = await GetAsync<UserPropertyResponse>($"api/v1/users/{userLogin}");
                var user = userResponse.data ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");

                if (user.status == "Lock")
                    throw new Exception($"Невозможно получить свойства, пользователь {userLogin} залочен");

                userProperties = user.FormUserProperties();
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetUserProperties: {e.Message}\n{e.StackTrace}");
                throw;
            }

            foreach (UserProperty userProperty in userProperties)
            {
                yield return userProperty;
            }
        }

        public async Task UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            try
            {
                var userResponse = await GetAsync<UserPropertyResponse>($"api/v1/users/{userLogin}");
                var user = userResponse.data ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");
                if (user.status == "Lock")
                    throw new Exception($"Невозможно обновить свойства, пользователь {userLogin} залочен");

                foreach (var property in properties)
                {
                    user.ChangeProperty(property);
                }

                var content = new StringContent(JsonSerializer.Serialize(user), UnicodeEncoding.UTF8, "application/json");
                await PutAsync<BaseResponse>("api/v1/users/edit", content);
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::UpdateUserProperties: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public bool IsUserExists(string userLogin)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var response = httpClient.GetAsync($"api/v1/users/all").Result;
            var userResponse = JsonSerializer.Deserialize<UserResponse>(response.Content.ReadAsStringAsync().Result);
            var user = userResponse.data.FirstOrDefault(_ => _.login == userLogin);

            if (user != null) return true;

            return false;
        }

        public void CreateUser(UserToCreate user)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var newUser = new CreateUSerDTO()
            {
                login = user.Login,
                password = user.HashPassword,

                lastName = user.Properties
                               .FirstOrDefault(p => p.Name.Equals("lastName", StringComparison.OrdinalIgnoreCase))
                               ?.Value ??
                           string.Empty,
                firstName = user.Properties
                                .FirstOrDefault(p => p.Name.Equals("firstName", StringComparison.OrdinalIgnoreCase))
                                ?.Value ??
                            string.Empty,
                middleName =
                    user.Properties.FirstOrDefault(p => p.Name.Equals("middleName", StringComparison.OrdinalIgnoreCase))
                        ?.Value ?? string.Empty,

                telephoneNumber =
                    user.Properties
                        .FirstOrDefault(p => p.Name.Equals("telephoneNumber", StringComparison.OrdinalIgnoreCase))
                        ?.Value ?? string.Empty,
                isLead = bool.TryParse(
                    user.Properties.FirstOrDefault(p => p.Name.Equals("isLead", StringComparison.OrdinalIgnoreCase))
                        ?.Value ?? string.Empty, out bool isLeadValue)
                    ? isLeadValue
                    : false,

                status = string.Empty
            };

            var content = new StringContent(JsonSerializer.Serialize(newUser), UnicodeEncoding.UTF8,
                "application/json");
            httpClient.PostAsync("api/v1/users/create", content).Wait();
        }

        private async Task<T?> GetAsync<T>(string path)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(path);
                response.EnsureSuccessStatusCode();
                return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::GetAsync: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        private async Task<T?> PostAsync<T, K>(string path, K data)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(data), UnicodeEncoding.UTF8, "application/json");
                
                HttpResponseMessage response = await _httpClient.PostAsync(path, content);
                response.EnsureSuccessStatusCode();
                return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::PostAsync: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        private async Task<T?> PutAsync<T, K>(string path, K data)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(data), UnicodeEncoding.UTF8, "application/json");
                
                HttpResponseMessage response = await _httpClient.PutAsync(path, content);
                response.EnsureSuccessStatusCode();
                return await JsonSerializer.DeserializeAsync<T>(await response.Content.ReadAsStreamAsync());
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::PutAsync: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        private async Task DeleteAsync(string path)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.DeleteAsync(path);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Logger.Error($"Connector::DeleteAsync: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}