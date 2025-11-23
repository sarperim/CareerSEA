using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class AuthService : IAuthService
    {
        public CareerSEADbContext _dbContext;
        public AuthService(CareerSEADbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<BaseResponse> RegisterAsync(SignupRequest request)
        {
            if (await _dbContext.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "User already exists"
                };
            }
            var user = new User
            {
                UserName = request.UserName,
                Name = request.Name,
                LastName = request.LastName
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            var response = new BaseResponse
            {
                Status = true,
                Data = user

            };

            return response;
        }

        public async Task<BaseResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user == null)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Wrong Username or Password"
                };

            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Wrong Username or Password"
                };

            }

            return new BaseResponse
            {
                Status = true,
                Message = "Successful",
                Data = await CreateTokenResponse(user)
            };
        }
        private async Task<TokenResponse> CreateTokenResponse(User? user)
        {
            return new TokenResponse
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }
        //!!!use keyvault in the deployment this functions isn't safe
        private string CreateToken(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MyVerySecureSecretKeyHere53278!!@#$%*^*^^*^%&!"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new JwtSecurityToken(
               issuer: "CareerSEA",
               audience:"MyUsers",
               claims: claims,
               expires: DateTime.UtcNow.AddDays(1),
               signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<String> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _dbContext.SaveChangesAsync();
            return refreshToken;
        }
    }
}
