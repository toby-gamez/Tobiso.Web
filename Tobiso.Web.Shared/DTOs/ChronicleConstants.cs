namespace Tobiso.Web.Shared.DTOs;

/// <summary>Known <c>ItemType</c> discriminator values for kronika items.</summary>
public static class ChronicleItemTypeConstants
{
    public const string Event = "Event";
    public const string Person = "Person";

    public static readonly IReadOnlyList<string> All = new[] { Event, Person };

    public static bool IsKnown(string? type) =>
        type != null && All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
