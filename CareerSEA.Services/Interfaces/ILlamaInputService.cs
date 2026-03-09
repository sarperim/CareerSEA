using CareerSEA.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface ILlamaInputService
    {
        Task<AIRequest?> ExtractCareerDataAsync(string cvText, CancellationToken cancellationToken = default);
    }
}
