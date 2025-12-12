using System.Security.Claims;
using CareerSEA.Contracts.DTOs;
using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Authorization;
/*
internal class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> DefaultTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authStateTask = DefaultTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<UserInfoDTO>(nameof(UserInfoDTO), out var userInfo) || userInfo is null)
            return;

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, UserInfoDTO.UserId),
            new Claim(ClaimTypes.Name,          UserInfoDTO.Name),
            new Claim(ClaimTypes.UserName,         UserInfoDTO.Username)
        ];

        _authStateTask = Task.FromResult(
            new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(claims, nameof(PersistentAuthenticationStateProvider)))));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authStateTask;
}*/