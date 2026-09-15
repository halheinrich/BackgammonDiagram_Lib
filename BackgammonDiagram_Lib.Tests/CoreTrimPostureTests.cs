using System.Reflection;
using BackgammonDiagram_Lib.Rendering;

namespace BackgammonDiagram_Lib.Tests;

/// <summary>
/// The declaration that makes the core's half of the trim gate a gate rather
/// than a suggestion (halheinrich/backgammon#197, in the mould of
/// XgFilter_Razor's posture pins for halheinrich/backgammon#193). The
/// analyzer's own verdict is enforced by the build under
/// TreatWarningsAsErrors; what this pins is that nobody quietly switches the
/// premise off — flipping IsTrimmable out of the core's csproj fails a test
/// rather than silently deferring the next trim-unsafe construct to BgQuiz's
/// trimmed publish. The ExportRaster sibling is deliberately not trimmable,
/// so the pin is anchored to a core type.
/// </summary>
public class CoreTrimPostureTests
{
    // IsTrimmable surfaces in the built assembly as SDK-emitted metadata,
    // which is the one trace of the csproj setting a test can read. The
    // analyzer switch beside it leaves no such trace; it is exercised instead
    // by the build itself, which fails on any call the analyzer flags as
    // trim-unsafe.
    [Fact]
    public void TheCoreAssembly_DeclaresItselfTrimmable()
    {
        Assert.Contains(
            typeof(DiagramRenderer).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>(),
            a => a.Key == "IsTrimmable" && a.Value == "True");
    }
}
