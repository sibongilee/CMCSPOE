using CMCSPOE.Controllers;
using CMCSPOE.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class ClaimsControllerTests
{
    [Fact]
    public void Submit_ValidClaim_RedirectsToIndex()
    {
        // Arrange: mock config and env (or pass real small config)
        var inMemorySettings = new Dictionary<string, string> { { "ConnectionStrings:DefaultConnection", "Server=(local);..." } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());

        var controller = new ClaimController(config, envMock.Object);

        var model = new Claim { LecturerId = 1, Month = DateTime.Today, HoursWorked = 5, HourlyRate = 100M };

        // Act
        var result = controller.SubmitClaim(model, null) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
    }
}