using Spectre.Console;
using Spectre.Console.Rendering;

namespace MCMPTools.Rendering;

public sealed class CountColumn : ProgressColumn
{
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        return new Markup($"{task.Value}[grey]/[/]{task.MaxValue}");
    }
}