namespace Tobiso.Web.Shared.DTOs;

public class SolverStep
{
    public string Step { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class StepSolverResponse
{
    public List<SolverStep> Steps { get; set; } = new();
}
