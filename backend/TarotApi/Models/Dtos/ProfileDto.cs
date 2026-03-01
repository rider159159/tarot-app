namespace TarotApi.Models.Dtos;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
