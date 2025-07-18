private class Model
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    private Model model_InputeUser = new Model();

    private async Task Authenticate()
    {
        User? userAccount = await accountServices.Authorization(model_InputeUser.UserName, model_InputeUser.Password);
        if (userAccount == null)
        {
            await js.InvokeVoidAsync("alert", "Не верный логин или пароль");
            return;
        }

        var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
        await customAuthStateProvider.UpdateAuthenticationState(new UserSession(userAccount.Id, userAccount.Name, userAccount.CAccessRank.Code));
        navManager.NavigateTo("/", true);
    }


using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;

namespace Server.Core.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage) => _sessionStorage = sessionStorage;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                //await Task.Delay(5000);
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;
                if (userSession == null)
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName),
                    new Claim(ClaimTypes.Role, userSession.Role)
                }, "CustomAuth"));
                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task UpdateAuthenticationState(UserSession? userSession)
        {
            ClaimsPrincipal claimsPrincipal;

            if (userSession != null)
            {
                await _sessionStorage.SetAsync("UserSession", userSession);
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName),
                    new Claim(ClaimTypes.Role, userSession.Role)
                }));
            }
            else
            {
                await _sessionStorage.DeleteAsync("UserSession");
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        /// <summary>
        /// Метод получения идентифкатора пользователя
        /// </summary>
        /// <returns>ID пользователя, -1 - если пользователь не определён</returns>
        public async Task<int> GetUserID()
        {
            var result = -1; // Сначала пользователь не определён
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession"); // Получаем хранилище сессии пользователя
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null; // Получаем сессию пользователя

                if (userSession is not null) // Если сессия существует, то выводим идентификатор пользователя
                    result = userSession.UserID;
            }
            catch { } // В случае ошибок игнорируем и выводим не определённого пользователя

            return result;
        }
    }
}
