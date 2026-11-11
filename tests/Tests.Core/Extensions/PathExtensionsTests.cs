using System;
using System.IO;
using Aurore.Foundation.Core.Extensions;

namespace Tests.Core.Extensions;

[Trait("Feature", "Runtime paths")]
public sealed class PathExtensionsTests
{
    [Fact(DisplayName = "On Windows a WSL path to a drive becomes the Windows path of that drive")]
    public void WslPathBecomesAWindowsPath()
    {
        // Arrange
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The WSL to Windows conversion only runs on Windows.");

        // Act
        var path = Path.NormalizeRuntimePath("/mnt/d/projects/app");

        // Assert
        Assert.Equal(@"D:\projects\app", path);
    }

    public static TheoryData<string, string> NotDrivePaths => new()
    {
        { "/mnt/", @"\mnt\" },
        { "/mnt/wsl/shared", @"\mnt\wsl\shared" },
        { "/mnt/1/data", @"\mnt\1\data" },
    };

    [Theory(DisplayName = "On Windows a /mnt path that is not a drive letter is not turned into a drive")]
    [MemberData(nameof(NotDrivePaths))]
    public void OnlyDriveLettersAreConverted(string input, string expectedEnd)
    {
        // Arrange
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The WSL to Windows conversion only runs on Windows.");

        // Act
        var path = Path.NormalizeRuntimePath(input);

        // Assert
        Assert.EndsWith(expectedEnd, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "On Windows the root of a WSL drive becomes the root of that drive")]
    public void DriveRootIsConverted()
    {
        // Arrange
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The WSL to Windows conversion only runs on Windows.");

        // Act
        var path = Path.NormalizeRuntimePath("/mnt/d");

        // Assert
        Assert.Equal(@"D:\", path);
    }

    [Fact(DisplayName = "On Windows a path that is already a Windows path is kept, and segments are combined")]
    public void WindowsPathIsKeptAndSegmentsAreCombined()
    {
        // Arrange
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The WSL to Windows conversion only runs on Windows.");

        // Act
        var path = Path.NormalizeRuntimePath(@"C:\data", "logs", "app.log");

        // Assert
        Assert.Equal(@"C:\data\logs\app.log", path);
    }
}
