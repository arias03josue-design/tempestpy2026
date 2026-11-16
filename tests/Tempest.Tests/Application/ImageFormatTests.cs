using System.Text;
using Tempest.Application.Common.Files;

namespace Tempest.Tests.Application;

public sealed class ImageFormatTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 }, ".jpg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, ".png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01 }, ".gif")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56 }, ".webp")]
    public void Detects_supported_formats_from_their_signature(byte[] header, string extension) =>
        Assert.Equal(extension, ImageFormat.Detect(header)?.Extension);

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"/>")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("%PDF-1.7")]
    [InlineData("")]
    public void Rejects_anything_else(string content) =>
        Assert.Null(ImageFormat.Detect(Encoding.UTF8.GetBytes(content)));
}
