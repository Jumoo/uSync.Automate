using NUnit.Framework;
using Umbraco.Automate.Core.Triggers;
using uSync.Automate.Actions.Triggers.Mapping;

namespace uSync.Automate.Tests.Mapping;

[TestFixture]
public class uSyncOriginBehaviourTests
{
    [TestCase("Run", AutomationOriginatedEventBehavior.Run)]
    [TestCase("skiponcycle", AutomationOriginatedEventBehavior.SkipOnCycle)]
    [TestCase("SkipAlways", AutomationOriginatedEventBehavior.SkipAlways)]
    public void Parse_ValidValue_IsCaseInsensitive(string value, AutomationOriginatedEventBehavior expected)
        => Assert.That(uSyncOriginBehaviour.Parse(value, AutomationOriginatedEventBehavior.Run), Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not-a-real-value")]
    public void Parse_InvalidValue_FallsBack(string? value)
        => Assert.That(uSyncOriginBehaviour.Parse(value, AutomationOriginatedEventBehavior.SkipAlways), Is.EqualTo(AutomationOriginatedEventBehavior.SkipAlways));
}
