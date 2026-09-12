namespace Tobiso.Web.App.Helpers;

// Czech nouns take one of three forms depending on count: 1 → singular,
// 2-4 → "few" plural, 0 and 5+ → "many" plural (genitive plural).
public static class CzechPlural
{
    public static string Of(int count, string one, string few, string many)
    {
        var word = count == 1 ? one : count is >= 2 and <= 4 ? few : many;
        return $"{count} {word}";
    }

    public static string Clanky(int count) => Of(count, "článek", "články", "článků");
    public static string Kategorie(int count) => Of(count, "kategorie", "kategorie", "kategorií");
    public static string Podkategorie(int count) => Of(count, "podkategorie", "podkategorie", "podkategorií");
    public static string Oblasti(int count) => Of(count, "oblast", "oblasti", "oblastí");
    public static string Otazky(int count) => Of(count, "otázka", "otázky", "otázek");
    public static string Odpovedi(int count) => Of(count, "odpověď", "odpovědi", "odpovědí");
    public static string Dny(int count) => Of(count, "den", "dny", "dní");
    public static string Kredity(int count) => Of(count, "kredit", "kredity", "kreditů");
    public static string Slova(int count) => Of(count, "slovo", "slova", "slov");
}
