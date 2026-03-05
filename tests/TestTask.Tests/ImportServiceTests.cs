using Moq;
using TestTask.Application.DTOs;
using TestTask.Application.Services;
using TestTask.Domain.Entities;
using Xunit;

namespace TestTask.Tests;

public class ImportServiceTests
{
    [Fact]
    public void MapToOffice_ShouldMapCorrectly()
    {
        // Arrange
        var city = new CityDto { Id = 1, Name = "Moscow" };
        var terminal = new TerminalDto
        {
            Id = "T1",
            Name = "Terminal 1",
            Address = "Street 1",
            Latitude = "55.75",
            Longitude = "37.61",
            WorkTime = "9-18",
            IsPVZ = true,
            Phones = new List<PhoneDto>
            {
                new PhoneDto { Number = "123", Comment = "Main" }
            }
        };

        // Act
        var office = ImportService.MapToOffice(terminal, city);

        // Assert
        Assert.Equal("T1", office.Code);
        Assert.Equal(1, office.CityCode);
        Assert.Equal("Moscow", office.AddressCity);
        Assert.Equal("Street 1", office.AddressStreet);
        Assert.Equal(OfficeType.PVZ, office.Type);
        Assert.Equal(55.75, office.Coordinates.Latitude);
        Assert.Equal(37.61, office.Coordinates.Longitude);
        Assert.Single(office.Phones);
        Assert.Equal("123", office.Phones.First().PhoneNumber);
    }
}
