using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
