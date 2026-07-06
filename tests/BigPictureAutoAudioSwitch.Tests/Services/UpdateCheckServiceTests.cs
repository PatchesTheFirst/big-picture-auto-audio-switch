using System.Net;
using System.Net.Http;
using System.Text;
using BigPictureAutoAudioSwitch.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.Services;

public class UpdateCheckServiceTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<UpdateCheckService>> _loggerMock;
    private readonly AppSettings _settings = new();

    public UpdateCheckServiceTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _settingsServiceMock.Setup(s => s.Settings).Returns(_settings);
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<UpdateCheckService>>();
    }

    private UpdateCheckService CreateService(HttpMessageHandler handler, string currentVersion = "1.0.1")
    {
        return new UpdateCheckService(
            _settingsServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object,
            handler,
            Version.Parse(currentVersion));
    }

    private static HttpMessageHandler JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    private static string ReleaseJson(string tagName, string htmlUrl = "https://github.com/owner/repo/releases/tag/v9.9.9")
        => $$"""{"tag_name": "{{tagName}}", "html_url": "{{htmlUrl}}"}""";

    [Fact]
    public async Task CheckForUpdate_WhenNewerVersionAvailable_ReturnsUpdateInfo()
    {
        // Arrange
        var releaseUrl = "https://github.com/owner/repo/releases/tag/v1.0.2";
        using var service = CreateService(JsonResponse(ReleaseJson("v1.0.2", releaseUrl)), currentVersion: "1.0.1");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.2");
        result.ReleaseUrl.Should().Be(releaseUrl);
        service.AvailableUpdate.Should().Be(result);
    }

    [Fact]
    public async Task CheckForUpdate_WhenNewerVersionAvailable_RaisesEventAndNotifies()
    {
        // Arrange
        using var service = CreateService(JsonResponse(ReleaseJson("v1.0.2")), currentVersion: "1.0.1");
        UpdateInfo? raisedInfo = null;
        service.UpdateAvailable += (_, info) => raisedInfo = info;

        // Act
        await service.CheckForUpdateAsync();

        // Assert
        raisedInfo.Should().NotBeNull();
        raisedInfo!.Version.Should().Be("1.0.2");
        _notificationServiceMock.Verify(
            n => n.ShowUpdateAvailable("1.0.2", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdate_NotifiesOnlyOncePerVersion()
    {
        // Arrange
        using var service = CreateService(JsonResponse(ReleaseJson("v1.0.2")), currentVersion: "1.0.1");

        // Act - check twice (simulates the daily re-check finding the same release)
        await service.CheckForUpdateAsync();
        await service.CheckForUpdateAsync();

        // Assert
        _notificationServiceMock.Verify(
            n => n.ShowUpdateAvailable(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdate_WhenSameVersion_ReturnsNull()
    {
        // Arrange - assembly versions have four parts (1.0.1.0), tags have three (v1.0.1)
        using var service = CreateService(JsonResponse(ReleaseJson("v1.0.1")), currentVersion: "1.0.1.0");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
        service.AvailableUpdate.Should().BeNull();
        _notificationServiceMock.Verify(
            n => n.ShowUpdateAvailable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CheckForUpdate_WhenOlderVersion_ReturnsNull()
    {
        // Arrange
        using var service = CreateService(JsonResponse(ReleaseJson("v1.0.0")), currentVersion: "1.0.1");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdate_WithTagWithoutVPrefix_ParsesVersion()
    {
        // Arrange
        using var service = CreateService(JsonResponse(ReleaseJson("1.2.0")), currentVersion: "1.0.1");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.2.0");
    }

    [Fact]
    public async Task CheckForUpdate_WithNonVersionTag_ReturnsNull()
    {
        // Arrange
        using var service = CreateService(JsonResponse(ReleaseJson("latest")), currentVersion: "1.0.1");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdate_WhenHttpErrorStatus_ReturnsNull()
    {
        // Arrange
        using var service = CreateService(JsonResponse("{}", HttpStatusCode.InternalServerError));

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdate_WhenRequestThrows_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"));
        using var service = CreateService(handler);

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdate_WithMalformedJson_ReturnsNull()
    {
        // Arrange
        using var service = CreateService(JsonResponse("not json at all"));

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdate_WhenReleaseUrlMissing_FallsBackToReleasesPage()
    {
        // Arrange
        using var service = CreateService(JsonResponse("""{"tag_name": "v1.0.2"}"""), currentVersion: "1.0.1");

        // Act
        var result = await service.CheckForUpdateAsync();

        // Assert
        result.Should().NotBeNull();
        result!.ReleaseUrl.Should().Contain("/releases/latest");
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
