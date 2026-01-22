namespace Tobiso.Web.Domain.Entities;

/// <summary>
/// Interaktivní cvičení vázané na článek.
/// Podporuje různé typy: obvody (circuit), časové osy (timeline), přetahování (drag-drop), molekuly (molecule), atd.
/// </summary>
public class InteractiveExercise
{
    public int Id { get; set; }
    
    /// <summary>
    /// ID článku, ke kterému cvičení patří
    /// </summary>
    public int PostId { get; set; }
    
    /// <summary>
    /// Název cvičení (např. "Zapoj sériový obvod", "Seřaď události na časové ose")
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Typ cvičení: "circuit", "timeline", "drag-drop", "molecule", "matching", atd.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON konfigurace cvičení (komponenty, prvky, možnosti...)
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON se správným řešením (pro validaci na backendu)
    /// </summary>
    public string SolutionJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Volitelné instrukce v Markdownu (zobrazí se nad cvičením)
    /// </summary>
    public string? InstructionsMarkdown { get; set; }
    
    /// <summary>
    /// Pořadí cvičení v rámci článku (pro správné řazení)
    /// </summary>
    public int OrderIndex { get; set; } = 0;
    
    /// <summary>
    /// Zda je cvičení aktivní (viditelné pro uživatele)
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Datum vytvoření
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Datum poslední úpravy
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigační vlastnost
    public Post Post { get; set; } = null!;
}
