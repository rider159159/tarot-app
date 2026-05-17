using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TarotApi.Data;
using TarotApi.Models;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/tarot")]
public class TarotController(ReadingService readingService) : ControllerBase
{
    // Public card catalog — useful both for the draw page (anonymous) and
    // for any future card browser. Cheap, static, no auth needed.
    [HttpGet("cards")]
    [AllowAnonymous]
    public ActionResult<List<TarotCardSummaryDto>> GetAllCards()
    {
        var cards = TarotCards.All.Select(c => new TarotCardSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            NameCht = c.NameCht,
            Arcana = c.Arcana,
            Suit = c.Suit,
            Number = c.Number
        }).ToList();

        return Ok(cards);
    }

    [HttpGet("cards/{id}")]
    [AllowAnonymous]
    public ActionResult<TarotCardDetailDto> GetCardById(string id)
    {
        var card = TarotCards.GetById(id);
        if (card is null)
            return NotFound(new ErrorResponseDto { Error = "找不到該塔羅牌", Code = "NOT_FOUND" });

        return Ok(new TarotCardDetailDto
        {
            Id = card.Id,
            Name = card.Name,
            NameCht = card.NameCht,
            Arcana = card.Arcana,
            Suit = card.Suit,
            Number = card.Number,
            MeaningUpright = card.MeaningUpright,
            MeaningReversed = card.MeaningReversed,
            Keywords = card.Keywords
        });
    }

    // Anonymous draw — no persistence, no auth. The client owns the result,
    // generates its own clientToken, and may later import it via
    // POST /api/readings/import once logged in.
    // Rate-limited per IP to deter scripted abuse (10r/min).
    [HttpPost("draw")]
    [AllowAnonymous]
    [EnableRateLimiting("anonymous-draw")]
    public ActionResult<AnonymousDrawResponseDto> Draw([FromBody] AnonymousDrawDto dto)
    {
        if (dto.SpreadType == SpreadType.Custom && dto.CardCount is null or < 1 or > 10)
            return BadRequest(new ErrorResponseDto
            {
                Error = "自定義牌陣張數需介於 1 到 10 之間",
                Code = "INVALID_CARD_COUNT"
            });

        var result = readingService.DrawAnonymous(dto.SpreadType, dto.Question, dto.CardCount ?? 0);
        return Ok(result);
    }
}
