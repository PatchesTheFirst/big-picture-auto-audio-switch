using BigPictureAutoAudioSwitch.Services;
using BigPictureAutoAudioSwitch.ViewModels;
using FluentAssertions;
using Moq;

namespace BigPictureAutoAudioSwitch.Tests.ViewModels;

public class TrayIconViewModelTests
{
    private readonly Mock<IBigPictureDetector> _detectorMock;

    public TrayIconViewModelTests()
    {
        _detectorMock = new Mock<IBigPictureDetector>();
    }

    [Fact]
    public void Constructor_InitializesStatusText()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);

        // Act
        var viewModel = new TrayIconViewModel(_detectorMock.Object);

        // Assert
        viewModel.StatusText.Should().Be("Monitoring for Big Picture");
    }

    [Fact]
    public void StatusText_WhenBigPictureActive_ShowsActiveMessage()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(true);

        // Act
        var viewModel = new TrayIconViewModel(_detectorMock.Object);

        // Assert
        viewModel.StatusText.Should().Be("Big Picture Mode Active");
    }

    [Fact]
    public void BigPictureStateChanged_UpdatesStatusText()
    {
        // Arrange
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);
        var viewModel = new TrayIconViewModel(_detectorMock.Object);
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
        var viewModel = new TrayIconViewModel(_detectorMock.Object);
        viewModel.StatusText.Should().Be("Big Picture Mode Active");

        // Simulate Big Picture deactivation
        _detectorMock.Setup(d => d.IsBigPictureActive).Returns(false);

        // Act - Raise the event
        _detectorMock.Raise(d => d.BigPictureStateChanged += null, _detectorMock.Object, false);

        // Assert
        viewModel.StatusText.Should().Be("Monitoring for Big Picture");
    }
}
