namespace Tobiso.Web.Shared.DTOs;

public class CreateGradeRequest
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

public class UpdateGradeRequest
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}
