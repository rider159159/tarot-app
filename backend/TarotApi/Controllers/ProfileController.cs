using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(ProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = User.GetUserId();
        var profile = await profileService.GetProfile(userId);

        if (profile is null) return NotFound();
        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] ProfileUpdateDto dto)
    {
        var userId = User.GetUserId();
        var profile = await profileService.UpdateProfile(userId, dto.DisplayName);

        if (profile is null) return NotFound();
        return Ok(profile);
    }
}
