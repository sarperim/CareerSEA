using CareerSEA.Contracts.Responses;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface ICvExtractionService
    {
        Task<BaseResponse> ExtractAsync(
            Stream pdfStream,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken);
    }
}
