namespace Tobiso.Web.Shared.DTOs;

public class FormulaVariable
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Min { get; set; }
    public double Max { get; set; }
    public double DefaultVal { get; set; }
    public double Step { get; set; } = 1;
}

public class FormulaEntry
{
    public string Formula { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string ResultVar { get; set; } = string.Empty;
    public string ResultUnit { get; set; } = string.Empty;
    public List<FormulaVariable> Variables { get; set; } = new();
}

public class FormulaVarsResponse
{
    public List<FormulaEntry> Formulas { get; set; } = new();
}
