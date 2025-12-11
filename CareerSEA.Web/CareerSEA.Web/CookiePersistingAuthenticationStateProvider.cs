using CareerSEA.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
/*
public sealed class CookiePersistingAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly Task<AuthenticationState> _authStateTask;

    public CookiePersistingAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        PersistentComponentState state)
    {
        var principal = httpContextAccessor.HttpContext?.User
                        ?? new ClaimsPrincipal(new ClaimsIdentity());

        _authStateTask = Task.FromResult(new AuthenticationState(principal));

        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.Identity.Name;

            if (!string.IsNullOrEmpty(userId))
            {
                state.PersistAsJson(nameof(UserInfo), new UserInfo
                {
                    UserId = userId,
                    Email = email ?? userId
                });
            }
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _authStateTask;
}*/