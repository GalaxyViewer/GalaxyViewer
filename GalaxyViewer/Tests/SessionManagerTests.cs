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
        mockLiteDbService.Setup(service => service.GetCollection<SessionModel>("sessions")).Returns(mockSessionCollection.Object);
        mockSessionCollection.Setup(collection => collection.FindOne(It.IsAny<Query>())).Returns(session);

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
        mockSessionCollection.Verify(collection => collection.Upsert(It.IsAny<SessionModel>()), Times.Once);
    }
}