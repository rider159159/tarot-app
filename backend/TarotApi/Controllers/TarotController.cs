using Microsoft.AspNetCore.Mvc;
using TarotApi.Data;
using TarotApi.Models.Dtos;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/tarot")]
public class TarotController : ControllerBase
{
    [HttpGet("cards")]
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
    public ActionResult<TarotCardDetailDto> GetCardById(string id)
    {
        var card = TarotCards.GetById(id);
        if (card is null) return NotFound();

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
}
