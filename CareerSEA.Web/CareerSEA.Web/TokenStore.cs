namespace CareerSEA.Web
{
    using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

    public class TokenStore
    {
        public string Username { get; set; }
        private readonly ProtectedSessionStorage _storage;
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";

        public TokenStore(ProtectedSessionStorage storage)
        {
            _storage = storage;
        }

        public async Task SetAccessTokenAsync(string token)
        {
            await _storage.SetAsync(AccessTokenKey, token);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var result = await _storage.GetAsync<string>(AccessTokenKey);
            return result.Success ? result.Value : null;
        }

        public async Task SetRefreshTokenAsync(string token)
        {
            await _storage.SetAsync(RefreshTokenKey, token);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            var result = await _storage.GetAsync<string>(RefreshTokenKey);
            return result.Success ? result.Value : null;
        }

        public async Task ClearAsync()
        {
            await _storage.DeleteAsync(AccessTokenKey);
            await _storage.DeleteAsync(RefreshTokenKey);
        }
    }

}
