using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(AdminService adminService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult> GetUsers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await adminService.ListUsers(search, page, pageSize);

        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        if (id == User.GetUserId())
            return BadRequest(new ErrorResponseDto
            {
                Error = "無法刪除自己的帳號",
                Code = "CANNOT_DELETE_SELF"
            });

        var deleted = await adminService.DeleteUser(id);
        if (!deleted)
            return NotFound(new ErrorResponseDto { Error = "找不到使用者", Code = "NOT_FOUND" });

        return NoContent();
    }
}
