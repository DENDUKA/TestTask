using System.Text.Json.Serialization;

namespace TestTask.Application.DTOs;

public record CityDto
{
    [JsonPropertyName("cityID")]
    public int? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("terminals")]
    public TerminalsDto? Terminals { get; set; }
}

public class TerminalsDto
{
    [JsonPropertyName("terminal")]
    public List<TerminalDto>? Terminal { get; set; }
}

public class TerminalDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("fullAddress")]
    public string FullAddress { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public string Latitude { get; set; } = string.Empty;

    [JsonPropertyName("longitude")]
    public string Longitude { get; set; } = string.Empty;

    [JsonPropertyName("phones")]
    public List<PhoneDto>? Phones { get; set; }

    [JsonPropertyName("workTime")]
    public string WorkTime { get; set; } = string.Empty;
    
    // Поля для определения типа
    [JsonPropertyName("isPVZ")]
    public bool IsPVZ { get; set; }
}

public class PhoneDto
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;
    
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class RootDto
{
    [JsonPropertyName("city")]
    public List<CityDto>? City { get; set; }
}
