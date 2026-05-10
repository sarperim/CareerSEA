using CareerSEA.Contracts.DTOs;
using CareerSEA.Contracts.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface ISavedItemsService
    {
        Task<BaseResponse> SaveJobAsync(Guid userId, JobListingDto job, CancellationToken cancellationToken = default);
        Task<BaseResponse> UnsaveJobAsync(Guid userId, Guid savedJobId, CancellationToken cancellationToken = default);
        Task<BaseResponse> SaveResourceAsync(Guid userId, ResourceItemDTO resource, CancellationToken cancellationToken = default);
        Task<BaseResponse> UnsaveResourceAsync(Guid userId, Guid savedResourceId, CancellationToken cancellationToken = default);
        Task<BaseResponse> GetSavedItemsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
