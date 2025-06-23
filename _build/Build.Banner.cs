
using System;
using System.Reflection;
using Spectre.Console;

public partial class Build
{
    public void RenderBanner(string text)
    {
        FigletFont LoadFont(string fontName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream($"FigletFonts.{fontName}.flf") ?? throw new InvalidOperationException("Can't load font from resource");
            var font = FigletFont.Load(stream);
            return font;
        }

        var banner = new FigletText(LoadFont("ANSIShadow"), text)
        {
            Pad = true
        };

        var grid = new Grid()
            .AddColumns(2);

        grid.Expand = false;

        grid.AddRow(banner);
        AnsiConsole.Write(grid);
    }
}