using BigPictureAutoAudioSwitch.Services;
using BigPictureAutoAudioSwitch.ViewModels;
using FluentAssertions;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.ViewModels;

public class TrayIconViewModelTests
{
    private readonly Mock<IBigPictureDetector> _detectorMock;
    private readonly Mock<IUpdateCheckService> _updateCheckServiceMock;

    public TrayIconViewModelTests()
    {
        _detectorMock = new Mock<IBigPictureDetector>();
        _updateCheckServiceMock = new Mock<IUpdateCheckService>();
    }

    private TrayIconViewModel CreateViewModel()
        => new(_detectorMock.Object, _updateCheckServiceMock.Object);

    [Fact]
    public void Constructor_InitializesStatusText()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.StatusText.Should().Be("Monitoring for Big Picture");
    }

    [Fact]
    public void StatusText_WhenBigPictureActive_ShowsActiveMessage()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.StatusText.Should().Be("Big Picture Mode Active");
    }

    [Fact]
    public void BigPictureStateChanged_UpdatesStatusText()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);
        var viewModel = CreateViewModel();
        viewModel.StatusText.Should().Be("Monitoring for Big Picture");

        // Simulate Big Picture activation
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(true);

        // Act - Raise the event
        _detectorMock.Raise(d => d.BigPictureStateChanged += null, _detectorMock.Object, true);

        // Assert
        viewModel.StatusText.Should().Be("Big Picture Mode Active");
    }

    [Fact]
    public void BigPictureStateChanged_WhenDeactivated_ShowsMonitoringMessage()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.StatusText.Should().Be("Big Picture Mode Active");

        // Simulate Big Picture deactivation
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);

        // Act - Raise the event
        _detectorMock.Raise(d => d.BigPictureStateChanged += null, _detectorMock.Object, false);

        // Assert
        viewModel.StatusText.Should().Be("Monitoring for Big Picture");
    }

    [Fact]
    public void Constructor_WhenNoUpdateAvailable_HidesUpdateMenuItem()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.UpdateAvailable.Should().BeFalse();
        viewModel.UpdateMenuHeader.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WhenUpdateAlreadyDetected_ShowsUpdateMenuItem()
    {
        // Arrange - service found an update before the view model was created
        var update = new UpdateInfo("1.0.2", "https://github.com/owner/repo/releases/tag/v1.0.2");
        _updateCheckServiceMock.Setup(u => u.AvailableUpdate).Returns(update);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.UpdateAvailable.Should().BeTrue();
        viewModel.UpdateMenuHeader.Should().Be("Update available (v1.0.2)...");
    }

    [Fact]
    public void UpdateAvailableEvent_ShowsUpdateMenuItem()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.UpdateAvailable.Should().BeFalse();
        var update = new UpdateInfo("1.0.2", "https://github.com/owner/repo/releases/tag/v1.0.2");

        // Act - Raise the event
        _updateCheckServiceMock.Raise(u => u.UpdateAvailable += null, _updateCheckServiceMock.Object, update);

        // Assert
        viewModel.UpdateAvailable.Should().BeTrue();
        viewModel.UpdateMenuHeader.Should().Be("Update available (v1.0.2)...");
    }
}
