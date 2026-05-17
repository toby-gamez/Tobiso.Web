namespace Tobiso.Web.Domain.Entities;

public class Grade
{
    // e.g. Id = 1, Name = "6. třída", Level = 6
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}
