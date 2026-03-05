using System.Text.Json.Serialization;

namespace TestTask.Entities;

public record Phone
{
    public int Id { get; set; }

    public int OfficeId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Additional { get; set; }

    [JsonIgnore]
    public Office Office { get; set; } = null!;
}
