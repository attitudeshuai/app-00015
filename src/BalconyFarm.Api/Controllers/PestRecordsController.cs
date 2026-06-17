using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;

namespace BalconyFarm.Api.Controllers;

[ApiController]
[Route("api/pestrecords")]
public class PestRecordsController : ControllerBase
{
    private readonly IPestRecordService _pestRecordService;

    public PestRecordsController(IPestRecordService pestRecordService)
    {
        _pestRecordService = pestRecordService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<PestRecordDto>>>> GetPestRecords([FromQuery] PestRecordQueryRequestDto query, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.GetPestRecordsAsync(query, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PestRecordDto>>> CreatePestRecord([FromBody] CreatePestRecordRequestDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.CreatePestRecordAsync(dto, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetPestRecordById), new { id = result.Data?.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PestRecordDto>>> GetPestRecordById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.GetPestRecordByIdAsync(id, userId, cancellationToken);
        if (result.Code != 200)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PestRecordDto>>> UpdatePestRecord([FromRoute] Guid id, [FromBody] UpdatePestRecordRequestDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.UpdatePestRecordAsync(id, dto, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeletePestRecord([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.DeletePestRecordAsync(id, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PestRecordDto>>> UpdatePestStatus([FromRoute] Guid id, [FromBody] UpdatePestStatusRequestDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.UpdatePestStatusAsync(id, dto, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{pestRecordId}/treatmentlogs")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PestRecordDto>>> AddTreatmentLog([FromRoute] Guid pestRecordId, [FromBody] CreateTreatmentLogRequestDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.AddTreatmentLogAsync(pestRecordId, dto, userId, cancellationToken);
        if (result.Code != 200)
        {
            if (result.Code == 404) return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{pestRecordId}/treatmentlogs")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<TreatmentLogDto>>>> GetTreatmentLogs([FromRoute] Guid pestRecordId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.GetTreatmentLogsAsync(pestRecordId, userId, cancellationToken);
        if (result.Code != 200)
        {
            if (result.Code == 404) return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("{pestRecordId}/treatmentlogs/{treatmentLogId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteTreatmentLog([FromRoute] Guid pestRecordId, [FromRoute] Guid treatmentLogId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _pestRecordService.DeleteTreatmentLogAsync(pestRecordId, treatmentLogId, userId, cancellationToken);
        if (result.Code != 200)
        {
            if (result.Code == 404) return NotFound(result);
            return BadRequest(result);
        }
        return Ok(result);
    }
}
