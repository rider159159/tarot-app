namespace TarotApi.Models.Dtos;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
