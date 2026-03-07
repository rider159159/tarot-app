namespace TarotApi.Models.Dtos;

public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Code { get; init; }
}
