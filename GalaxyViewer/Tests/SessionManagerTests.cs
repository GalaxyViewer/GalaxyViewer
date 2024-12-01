using GalaxyViewer.Models;
using GalaxyViewer.Services;
using LiteDB;
using Moq;
using Xunit;

namespace GalaxyViewer.Tests;

public class SessionManagerTests
{
    [Fact]
    public void SessionManager_Should_SaveSession_When_SessionChanges()
    {
        // Arrange
        var mockLiteDbService = new Mock<ILiteDbService>();
        var session = new SessionModel
        {
            IsLoggedIn = false,
            AvatarName = "TestAvatar",
            AvatarKey = new OpenMetaverse.UUID(),
            Balance = 100,
            CurrentLocation = "TestLocation",
            CurrentLocationWelcomeMessage = "Welcome to TestLocation"
        };

        var mockSessionCollection = new Mock<ILiteCollection<SessionModel>>();
        mockLiteDbService.Setup(service => service.GetCollection<SessionModel>("session"))
            .Returns(mockSessionCollection.Object);
        mockLiteDbService.Setup(service => service.GetSession()).Returns(session);
        mockLiteDbService.Setup(service => service.SaveSession(It.IsAny<SessionModel>()))
            .Callback<SessionModel>(s => mockSessionCollection.Object.Upsert(s));

        var sessionManager = new SessionManager(mockLiteDbService.Object);

        // Act
        sessionManager.Session = new SessionModel
        {
            IsLoggedIn = true,
            AvatarName = "TestAvatar",
            AvatarKey = new OpenMetaverse.UUID(),
            Balance = 100,
            CurrentLocation = "TestLocation",
            CurrentLocationWelcomeMessage = "Welcome to TestLocation"
        };

        // Assert
        mockSessionCollection.Verify(collection => collection.Upsert(It.IsAny<SessionModel>()),
            Times.Once);
    }
}