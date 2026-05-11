using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;


[ApiController]
[Route("api/readings")]
public class ReadingController(
    ReadingService readingService,
    PromptBuilder promptBuilder) : ControllerBase
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

    [HttpGet("weekly-fortune")]
    public async Task<ActionResult> GetWeeklyFortune()
    {
        var userId = User.GetUserId();
        var result = await readingService.GetWeeklyFortune(userId);
        return Ok(new { reading = result, canDraw = result is null });
    }

    [HttpPost("weekly-fortune")]
    public async Task<ActionResult<ReadingResponseDto>> CreateWeeklyFortune()
    {
        var userId = User.GetUserId();
        try
        {
            var result = await readingService.CreateWeeklyFortune(userId);
            return Created($"/api/readings/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Error = ex.Message, Code = "WEEKLY_LIMIT" });
        }
    }

    [HttpGet("export")]
    public async Task<ActionResult<ExportBatchDto>> ExportAll()
    {
        const int hardLimit = 1000;
        var userId = User.GetUserId();
        var (items, totalCount, isTruncated) = await readingService.GetReadingsForExport(userId, hardLimit);
        return Ok(promptBuilder.BuildBatchExport(items, totalCount, isTruncated));
    }

    [HttpGet("{id:guid}/export")]
    public async Task<ActionResult<ExportPayloadDto>> ExportSingle(Guid id)
    {
        var userId = User.GetUserId();
        var reading = await readingService.GetRawReadingById(userId, id);
        if (reading is null)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return Ok(promptBuilder.BuildSingleExport(reading));
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
