using DevFlow.Application.Common;
using DevFlow.Application.Issues;
using DevFlow.Application.Projects;

namespace DevFlow.UnitTests.Application.Common;

public sealed class GuardAndSanitizationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_RejectsBlank(string? value) =>
        Assert.ThrowsAny<ArgumentException>(() => Guard.Required(value, "Name"));

    [Fact]
    public void Required_TrimsAndEnforcesMaxLength()
    {
        Assert.Equal("abc", Guard.Required("  abc  ", "Name", 3));
        Assert.ThrowsAny<ArgumentException>(() => Guard.Required("abcd", "Name", 3));
    }

    [Fact]
    public void Optional_ReturnsNullForBlank() => Assert.Null(Guard.Optional("  "));

    [Fact]
    public void ValidDateRange_RejectsEndBeforeStart()
    {
        var start = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.ThrowsAny<ArgumentException>(() => Guard.ValidDateRange(start, start.AddDays(-1)));
        Guard.ValidDateRange(start, start);
        Guard.ValidDateRange(null, start);
    }

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    [InlineData("C:\\temp\\notes.txt", "notes.txt")]
    [InlineData("my file (1).png", "my file (1).png")]
    public void SanitizeFileName_StripsPaths(string input, string expected) =>
        Assert.Equal(expected, AttachmentManagementService.SanitizeFileName(input));

    [Theory]
    [InlineData("")]
    [InlineData("...")]
    public void SanitizeFileName_RejectsEmptyNames(string input) =>
        Assert.ThrowsAny<ArgumentException>(() => AttachmentManagementService.SanitizeFileName(input));

    [Fact]
    public void SanitizeFileName_TruncatesButKeepsExtension()
    {
        var result = AttachmentManagementService.SanitizeFileName(new string('a', 300) + ".pdf");
        Assert.Equal(200, result.Length);
        Assert.EndsWith(".pdf", result);
    }

    [Theory]
    [InlineData(".html")]
    [InlineData(".svg")]
    [InlineData(".js")]
    [InlineData(".exe")]
    public void AllowedContentTypes_ExcludeActiveContent(string extension) =>
        Assert.False(AttachmentManagementService.AllowedContentTypes.ContainsKey(extension));

    [Theory]
    [InlineData("web", "WEB")]
    [InlineData(" Api2 ", "API2")]
    public void NormalizeKey_UppercasesValidKeys(string input, string expected) =>
        Assert.Equal(expected, ProjectManagementService.NormalizeKey(input));

    [Theory]
    [InlineData("A")]
    [InlineData("TOO-LONG-KEY")]
    [InlineData("has space")]
    public void NormalizeKey_RejectsInvalidKeys(string input) =>
        Assert.ThrowsAny<ArgumentException>(() => ProjectManagementService.NormalizeKey(input));
}
