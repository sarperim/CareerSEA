using CareerSEA.Contracts.DTOs;
using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SavedItemsController : ControllerBase
    {
        private readonly ISavedItemsService _savedItemsService;

        public SavedItemsController(ISavedItemsService savedItemsService)
        {
            _savedItemsService = savedItemsService;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponse>> Get(CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await _savedItemsService.GetSavedItemsAsync(userId, cancellationToken);
            return Ok(response);
        }

        [HttpPost("jobs")]
        public async Task<ActionResult<BaseResponse>> SaveJob([FromBody] JobListingDto job, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await _savedItemsService.SaveJobAsync(userId, job, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("jobs/{id:guid}")]
        public async Task<ActionResult<BaseResponse>> UnsaveJob(Guid id, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await _savedItemsService.UnsaveJobAsync(userId, id, cancellationToken);
            return Ok(response);
        }

        [HttpPost("resources")]
        public async Task<ActionResult<BaseResponse>> SaveResource([FromBody] ResourceItemDTO resource, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await _savedItemsService.SaveResourceAsync(userId, resource, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("resources/{id:guid}")]
        public async Task<ActionResult<BaseResponse>> UnsaveResource(Guid id, CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var response = await _savedItemsService.UnsaveResourceAsync(userId, id, cancellationToken);
            return Ok(response);
        }

        private bool TryGetUserId(out Guid userId)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out userId);
        }
    }
}
