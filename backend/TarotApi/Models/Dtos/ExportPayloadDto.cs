namespace TarotApi.Models.Dtos;

public class ExportPayloadDto
{
    public ExportMetadataDto Metadata { get; set; } = null!;
    public ExportReadingDto Reading { get; set; } = null!;
    public string AiPromptSuggestion { get; set; } = string.Empty;
}

public class ExportBatchDto
{
    public ExportMetadataDto Metadata { get; set; } = null!;
    public int TotalCount { get; set; }
    public int ExportedCount { get; set; }
    public bool IsTruncated { get; set; }
    public List<ExportReadingDto> Readings { get; set; } = [];
}

public class ExportMetadataDto
{
    public DateTime ExportedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string FormatVersion { get; set; } = string.Empty;
}

public class ExportReadingDto
{
    public Guid Id { get; set; }
    public DateTime DrawnAt { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string SpreadTypeKey { get; set; } = string.Empty;
    public string? Question { get; set; }
    public List<ExportCardDto> Cards { get; set; } = [];
}

public class ExportCardDto
{
    public string Position { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Orientation { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
    public string Meaning { get; set; } = string.Empty;
}
