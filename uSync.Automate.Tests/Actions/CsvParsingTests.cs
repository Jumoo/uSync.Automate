using NUnit.Framework;
using uSync.Automate.Actions.Actions;

namespace uSync.Automate.Tests.Actions;

[TestFixture]
public class CsvParsingTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ParseOrNull_BlankInput_ReturnsNull(string? input)
        => Assert.That(CsvParsing.ParseOrNull(input), Is.Null);

    [Test]
    public void ParseOrNull_TrimsAndSplits()
    {
        var result = CsvParsing.ParseOrNull(" contentHandler , dataTypeHandler ,,mediaHandler");
        Assert.That(result, Is.EqualTo(new[] { "contentHandler", "dataTypeHandler", "mediaHandler" }));
    }
}
