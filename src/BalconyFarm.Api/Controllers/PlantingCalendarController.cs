using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Models;
using BalconyFarm.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BalconyFarm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantingCalendarController : ControllerBase
{
    private readonly IPlantingCalendarService _plantingCalendarService;

    public PlantingCalendarController(IPlantingCalendarService plantingCalendarService)
    {
        _plantingCalendarService = plantingCalendarService;
    }

    [HttpGet("cities")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CityDto>>>> GetAvailableCities(CancellationToken cancellationToken)
    {
        var result = await _plantingCalendarService.GetAvailableCitiesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("recommendations")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PlantingCalendarResponseDto>>> GetRecommendations([FromQuery] GetRecommendationsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _plantingCalendarService.GetRecommendationsAsync(request, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("create-crop")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CropDto>>> CreateCropFromRecommendation([FromBody] CreateCropFromRecommendationRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Error("用户未认证", 401));
        }

        var result = await _plantingCalendarService.CreateCropFromRecommendationAsync(request, userId, cancellationToken);
        if (result.Code != 200)
        {
            return BadRequest(result);
        }
        return CreatedAtAction(nameof(GetRecommendations), new { city = request.City, month = request.Month }, result);
    }
}
