using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using CareerSEA.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public readonly IAuthService _authService;
        public AuthController(IAuthService authService) 
        {
            this._authService = authService;
        }


        [HttpPost("Register")]
        public async Task<ActionResult<BaseResponse>> Register(SignupRequest request)
        {
            var user = await _authService.RegisterAsync(request);
            if (user.Status == false)
            {
                return BadRequest("User Exist.");
            }
            return Ok(user);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<BaseResponse>> Login(LoginRequest request)
        {
            var tokens = await _authService.LoginAsync(request);
            if (tokens == null)
            {
                return BadRequest("Invalid Username or Password");
            }
            return Ok(tokens);
        }

    }
}
