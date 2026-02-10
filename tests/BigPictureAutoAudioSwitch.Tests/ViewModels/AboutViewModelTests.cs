using BigPictureAutoAudioSwitch.ViewModels;
using FluentAssertions;

namespace BigPictureAutoAudioSwitch.Tests.ViewModels;

public class AboutViewModelTests
{
    [Fact]
    public void AppName_ReturnsCorrectName()
    {
        // Arrange
        var viewModel = new AboutViewModel();

        // Act & Assert
        viewModel.AppName.Should().Be("Big Picture Auto Audio Switch");
    }

    [Fact]
    public void Version_ReturnsNonEmptyString()
    {
        // Arrange
        var viewModel = new AboutViewModel();

        // Act & Assert
        viewModel.Version.Should().NotBeNullOrEmpty();
        viewModel.Version.Should().StartWith("Version");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Arrange
        var viewModel = new AboutViewModel();

        // Act & Assert
        viewModel.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Copyright_ContainsCurrentYear()
    {
        // Arrange
        var viewModel = new AboutViewModel();

        // Act & Assert
        viewModel.Copyright.Should().Contain(DateTime.Now.Year.ToString());
    }

    [Fact]
    public void GitHubUrl_IsValidUrl()
    {
        // Arrange
        var viewModel = new AboutViewModel();

        // Act & Assert
        viewModel.GitHubUrl.Should().StartWith("https://");
    }
}
