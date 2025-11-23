using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface IAuthService
    {
        public Task<BaseResponse> RegisterAsync(SignupRequest request);
    }
}
