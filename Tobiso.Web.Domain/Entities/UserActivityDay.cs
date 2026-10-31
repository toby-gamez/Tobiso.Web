namespace Tobiso.Web.Domain.Entities;

// "The user was active on this calendar day" (Europe/Prague date, picked by the server). One row per
// (user, day); the composite key is the idempotency guard for POST /api/user/activity.
public class UserActivityDay
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }

    public AppUser User { get; set; } = null!;
}
