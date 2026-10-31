namespace Tobiso.Web.App.Admin.Components.Shared;

/// <summary>Colour choices shared by the flowchart and Venn builders (names GraphRenderer.ResolveColor knows).</summary>
internal static class BuilderColors
{
    public static readonly (string Value, string Label)[] All =
    [
        ("RED", "Červená"), ("DARK_RED", "Tmavě červená"),
        ("BLUE", "Modrá"), ("LIGHT_BLUE", "Světle modrá"), ("DARK_BLUE", "Tmavě modrá"),
        ("GREEN", "Zelená"), ("LIGHT_GREEN", "Světle zelená"), ("DARK_GREEN", "Tmavě zelená"),
        ("ORANGE", "Oranžová"), ("YELLOW", "Žlutá"), ("PURPLE", "Fialová"), ("PINK", "Růžová"),
        ("BROWN", "Hnědá"), ("GRAY", "Šedá"), ("BLACK", "Černá (barva textu)"),
    ];
}
