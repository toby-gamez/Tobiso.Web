using System.Text.Json.Serialization;

namespace Tobiso.Web.Domain.Entities;

/// <summary>A name on a class's duty roster. Plain text - deliberately not linked to a user account.</summary>
public class DutyPerson
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>Two people who share the duty for a week. <see cref="Position"/> is the order of the rotation.</summary>
public class DutyPair
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    public int Position { get; set; }
    public int PersonAId { get; set; }
    public DutyPerson PersonA { get; set; } = null!;
    public int PersonBId { get; set; }
    public DutyPerson PersonB { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter<DutyWeekKind>))]
public enum DutyWeekKind { Regular, Penalty, Off }

/// <summary>One week (Monday to Friday) of a class's duty schedule.</summary>
public class DutyWeek
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    /// <summary>The Monday of the week.</summary>
    public DateOnly WeekStart { get; set; }
    /// <summary>Null for a week without duty.</summary>
    public int? PairId { get; set; }
    public DutyPair? Pair { get; set; }
    public DutyWeekKind Kind { get; set; } = DutyWeekKind.Regular;
}
