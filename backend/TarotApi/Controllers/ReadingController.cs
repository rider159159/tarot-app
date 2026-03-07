using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;


[ApiController]
[Route("api/readings")]
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var userId = User.GetUserId();
        var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
        return Created($"/api/readings/{result.Id}", result);
    }

    [HttpGet]
    public async Task<ActionResult> GetReadings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var userId = User.GetUserId();
        var (items, totalCount) = await readingService.GetReadings(userId, page, pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReadingResponseDto>> GetReadingById(Guid id)
    {
        var userId = User.GetUserId();
        var result = await readingService.GetReadingById(userId, id);

        if (result is null)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ReadingStatsDto>> GetStats()
    {
        var userId = User.GetUserId();
        var stats = await readingService.GetStats(userId);
        return Ok(stats);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteReading(Guid id)
    {
        var userId = User.GetUserId();
        var deleted = await readingService.DeleteReading(userId, id);

        if (!deleted)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return NoContent();
    }
}
