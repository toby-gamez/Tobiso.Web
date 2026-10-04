namespace Tobiso.Web.App.Helpers;

// Dates are stored as UTC in the DB; the server renders a UTC ISO-8601 timestamp into
// data-utc-date so client-side JS (localizeDates() in lucide-init.js) can re-format it
// in the viewer's own time zone.
public static class LocalDate
{
    public static string ToUtcAttribute(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("o");
}
