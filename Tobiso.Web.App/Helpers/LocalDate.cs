namespace Tobiso.Web.App.Helpers;

// Post/category timestamps are stored (and come back from EF) as UTC values with
// DateTimeKind.Unspecified, so DateTime.ToString("o") would omit the "Z" suffix and
// window.localizeDates() in lucide-init.js would then have the browser's Date parser
// treat the string as LOCAL time instead of UTC. Force Kind=Utc before formatting so
// the timestamp round-trips into JS unambiguously.
public static class LocalDate
{
    public static string ToUtcAttribute(DateTime dt) =>
        DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("o");
}
