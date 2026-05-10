using CareerSEA.Contracts.DTOs;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class SavedItemsService : ISavedItemsService
    {
        private readonly CareerSEADbContext _dbContext;
        private readonly ILogger<SavedItemsService> _logger;

        public SavedItemsService(
            CareerSEADbContext dbContext,
            ILogger<SavedItemsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<BaseResponse> SaveJobAsync(Guid userId, JobListingDto job, CancellationToken cancellationToken = default)
        {
            if (job == null || string.IsNullOrWhiteSpace(job.Link))
            {
                return new BaseResponse { Status = false, Message = "A job link is required to save." };
            }

            try
            {
                var existing = await _dbContext.SavedJobs
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Link == job.Link, cancellationToken);

                if (existing != null)
                {
                    return new BaseResponse
                    {
                        Status = true,
                        Message = "Job is already saved.",
                        Data = ToDto(existing)
                    };
                }

                var entity = new SavedJob
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = (job.Title ?? string.Empty).Trim(),
                    Company = (job.Company ?? string.Empty).Trim(),
                    Location = (job.Location ?? string.Empty).Trim(),
                    Link = job.Link.Trim(),
                    SavedAt = DateTime.UtcNow
                };

                await _dbContext.SavedJobs.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BaseResponse
                {
                    Status = true,
                    Message = "Job saved.",
                    Data = ToDto(entity)
                };
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogWarning(ex, "Duplicate save attempt for job {Link} by user {UserId}.", job.Link, userId);
                var existing = await _dbContext.SavedJobs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Link == job.Link, cancellationToken);

                if (existing != null)
                {
                    return new BaseResponse
                    {
                        Status = true,
                        Message = "Job is already saved.",
                        Data = ToDto(existing)
                    };
                }

                return new BaseResponse { Status = false, Message = "Could not save the job." };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save job {Link} for user {UserId}.", job.Link, userId);
                return new BaseResponse { Status = false, Message = "An error occurred while saving the job." };
            }
        }

        public async Task<BaseResponse> UnsaveJobAsync(Guid userId, Guid savedJobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _dbContext.SavedJobs
                    .FirstOrDefaultAsync(s => s.Id == savedJobId && s.UserId == userId, cancellationToken);

                if (entity == null)
                {
                    return new BaseResponse { Status = false, Message = "Saved job not found." };
                }

                _dbContext.SavedJobs.Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BaseResponse { Status = true, Message = "Job removed." };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to unsave job {SavedJobId} for user {UserId}.", savedJobId, userId);
                return new BaseResponse { Status = false, Message = "An error occurred while removing the saved job." };
            }
        }

        public async Task<BaseResponse> SaveResourceAsync(Guid userId, ResourceItemDTO resource, CancellationToken cancellationToken = default)
        {
            if (resource == null || string.IsNullOrWhiteSpace(resource.Url))
            {
                return new BaseResponse { Status = false, Message = "A resource URL is required to save." };
            }

            try
            {
                var existing = await _dbContext.SavedResources
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Url == resource.Url, cancellationToken);

                if (existing != null)
                {
                    return new BaseResponse
                    {
                        Status = true,
                        Message = "Resource is already saved.",
                        Data = ToDto(existing)
                    };
                }

                var entity = new SavedResource
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = (resource.Title ?? string.Empty).Trim(),
                    Url = resource.Url.Trim(),
                    Snippet = (resource.Snippet ?? string.Empty).Trim(),
                    Provider = (resource.Provider ?? string.Empty).Trim(),
                    Skill = (resource.Skill ?? string.Empty).Trim(),
                    SavedAt = DateTime.UtcNow
                };

                await _dbContext.SavedResources.AddAsync(entity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BaseResponse
                {
                    Status = true,
                    Message = "Resource saved.",
                    Data = ToDto(entity)
                };
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogWarning(ex, "Duplicate save attempt for resource {Url} by user {UserId}.", resource.Url, userId);
                var existing = await _dbContext.SavedResources
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Url == resource.Url, cancellationToken);

                if (existing != null)
                {
                    return new BaseResponse
                    {
                        Status = true,
                        Message = "Resource is already saved.",
                        Data = ToDto(existing)
                    };
                }

                return new BaseResponse { Status = false, Message = "Could not save the resource." };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save resource {Url} for user {UserId}.", resource.Url, userId);
                return new BaseResponse { Status = false, Message = "An error occurred while saving the resource." };
            }
        }

        public async Task<BaseResponse> UnsaveResourceAsync(Guid userId, Guid savedResourceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _dbContext.SavedResources
                    .FirstOrDefaultAsync(s => s.Id == savedResourceId && s.UserId == userId, cancellationToken);

                if (entity == null)
                {
                    return new BaseResponse { Status = false, Message = "Saved resource not found." };
                }

                _dbContext.SavedResources.Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new BaseResponse { Status = true, Message = "Resource removed." };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to unsave resource {SavedResourceId} for user {UserId}.", savedResourceId, userId);
                return new BaseResponse { Status = false, Message = "An error occurred while removing the saved resource." };
            }
        }

        public async Task<BaseResponse> GetSavedItemsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var jobs = await _dbContext.SavedJobs
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.SavedAt)
                    .ToListAsync(cancellationToken);

                var resources = await _dbContext.SavedResources
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.SavedAt)
                    .ToListAsync(cancellationToken);

                var grouped = resources
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.Skill) ? "Other" : r.Skill, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new SavedResourceGroupDTO
                    {
                        Skill = g.Key,
                        Resources = g.Select(ToDto).ToList()
                    })
                    .OrderBy(g => g.Skill, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var payload = new SavedItemsResponse
                {
                    Jobs = jobs.Select(ToDto).ToList(),
                    Resources = grouped
                };

                return new BaseResponse
                {
                    Status = true,
                    Message = "Success",
                    Data = payload
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load saved items for user {UserId}.", userId);
                return new BaseResponse { Status = false, Message = "An error occurred while loading saved items." };
            }
        }

        private static SavedJobDTO ToDto(SavedJob entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Company = entity.Company,
            Location = entity.Location,
            Link = entity.Link,
            SavedAt = entity.SavedAt
        };

        private static SavedResourceDTO ToDto(SavedResource entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Url = entity.Url,
            Snippet = entity.Snippet,
            Provider = entity.Provider,
            Skill = entity.Skill,
            SavedAt = entity.SavedAt
        };
    }
}
