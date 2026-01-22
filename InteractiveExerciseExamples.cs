using System.Text.Json;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Tests;

/// <summary>
/// Příklady JSON dat pro testování interaktivních cvičení
/// </summary>
public static class InteractiveExerciseExamples
{
    /// <summary>
    /// Příklad: Fyzikální obvod - zapojení žárovky s baterií a přepínačem
    /// </summary>
    public static CreateInteractiveExerciseRequest CircuitExample(int postId)
    {
        var config = new
        {
            type = "circuit",
            components = new[]
            {
                new { id = "battery-1", type = "battery", voltage = 12, x = 50, y = 100 },
                new { id = "bulb-1", type = "bulb", resistance = 6, x = 200, y = 100 },
                new { id = "switch-1", type = "switch", state = "off", x = 350, y = 100 }
            },
            availableComponents = new[]
            {
                new { type = "bulb", label = "Žárovka" },
                new { type = "resistor", label = "Odpor" },
                new { type = "switch", label = "Přepínač" },
                new { type = "wire", label = "Vodič" }
            }
        };

        var solution = new
        {
            correctConnections = new[]
            {
                new { from = "battery-1", to = "switch-1" },
                new { from = "switch-1", to = "bulb-1" },
                new { from = "bulb-1", to = "battery-1" }
            },
            explanation = "Správně zapojený sériový obvod: baterie → přepínač → žárovka → zpět k baterii."
        };

        return new CreateInteractiveExerciseRequest
        {
            PostId = postId,
            Title = "Zapoj sériový obvod",
            Type = "circuit",
            ConfigJson = JsonSerializer.Serialize(config),
            SolutionJson = JsonSerializer.Serialize(solution),
            InstructionsMarkdown = @"
## Úkol
Zapoj jednoduchý obvod s jednou žárovkou, baterií a přepínačem.

**Cíl:** Žárovka se rozsvítí, když zapneš přepínač.

**Komponenty:**
- 1× baterie (12V)
- 1× žárovka
- 1× přepínač
",
            OrderIndex = 0,
            IsActive = true
        };
    }

    /// <summary>
    /// Příklad: Časová osa - české historické události
    /// </summary>
    public static CreateInteractiveExerciseRequest TimelineExample(int postId)
    {
        var config = new
        {
            type = "timeline",
            events = new[]
            {
                new { id = "event-1", label = "Bitva u Lipan", year = 1434 },
                new { id = "event-2", label = "Bitva na Bílé hoře", year = 1620 },
                new { id = "event-3", label = "Založení Karlovy univerzity", year = 1348 },
                new { id = "event-4", label = "Bitva u Mohelnice", year = 1469 },
                new { id = "event-5", label = "Defenestrace pražská", year = 1618 }
            },
            timeRange = new { start = 1300, end = 1700 }
        };

        var solution = new
        {
            correctOrder = new[] { "event-3", "event-1", "event-4", "event-5", "event-2" },
            explanation = "Události v chronologickém pořadí: 1348 → 1434 → 1469 → 1618 → 1620"
        };

        return new CreateInteractiveExerciseRequest
        {
            PostId = postId,
            Title = "Seřaď historické události",
            Type = "timeline",
            ConfigJson = JsonSerializer.Serialize(config),
            SolutionJson = JsonSerializer.Serialize(solution),
            InstructionsMarkdown = "Přetáhni události na časovou osu ve správném chronologickém pořadí.",
            OrderIndex = 1,
            IsActive = true
        };
    }

    /// <summary>
    /// Příklad: Přetahování slov - slovní druhy
    /// </summary>
    public static CreateInteractiveExerciseRequest DragDropExample(int postId)
    {
        var config = new
        {
            type = "drag-drop",
            items = new[]
            {
                new { id = "word-1", text = "pes" },
                new { id = "word-2", text = "rychlý" },
                new { id = "word-3", text = "běží" },
                new { id = "word-4", text = "zahrada" },
                new { id = "word-5", text = "pomalu" },
                new { id = "word-6", text = "zelený" }
            },
            categories = new[]
            {
                new { id = "noun", label = "Podstatné jméno" },
                new { id = "adjective", label = "Přídavné jméno" },
                new { id = "verb", label = "Sloveso" },
                new { id = "adverb", label = "Příslovce" }
            }
        };

        var solution = new
        {
            correctPlacements = new Dictionary<string, string>
            {
                { "word-1", "noun" },      // pes
                { "word-2", "adjective" }, // rychlý
                { "word-3", "verb" },      // běží
                { "word-4", "noun" },      // zahrada
                { "word-5", "adverb" },    // pomalu
                { "word-6", "adjective" }  // zelený
            },
            explanation = "Správné zařazení: podstatná jména (pes, zahrada), přídavná jména (rychlý, zelený), sloveso (běží), příslovce (pomalu)."
        };

        return new CreateInteractiveExerciseRequest
        {
            PostId = postId,
            Title = "Urči slovní druhy",
            Type = "drag-drop",
            ConfigJson = JsonSerializer.Serialize(config),
            SolutionJson = JsonSerializer.Serialize(solution),
            InstructionsMarkdown = "Přetáhni každé slovo do správné kategorie slovního druhu.",
            OrderIndex = 2,
            IsActive = true
        };
    }

    /// <summary>
    /// Příklad: Chemická molekula - voda (H₂O)
    /// </summary>
    public static CreateInteractiveExerciseRequest MoleculeExample(int postId)
    {
        var config = new
        {
            type = "molecule",
            availableAtoms = new[]
            {
                new { symbol = "H", count = 4, name = "Vodík" },
                new { symbol = "O", count = 2, name = "Kyslík" },
                new { symbol = "C", count = 2, name = "Uhlík" }
            },
            instructions = "Sestav molekulu vody (H₂O)"
        };

        var solution = new
        {
            correctAtoms = new[]
            {
                new { id = "atom-1", symbol = "H", bonds = 1 },
                new { id = "atom-2", symbol = "O", bonds = 2 },
                new { id = "atom-3", symbol = "H", bonds = 1 }
            },
            bonds = new[]
            {
                new { from = "atom-1", to = "atom-2", type = "single" },
                new { from = "atom-2", to = "atom-3", type = "single" }
            },
            explanation = "Voda (H₂O): jeden atom kyslíku spojený s dvěma atomy vodíku jednoduchými vazbami."
        };

        return new CreateInteractiveExerciseRequest
        {
            PostId = postId,
            Title = "Sestav molekulu vody",
            Type = "molecule",
            ConfigJson = JsonSerializer.Serialize(config),
            SolutionJson = JsonSerializer.Serialize(solution),
            InstructionsMarkdown = @"
## Chemický vzorec: H₂O

Zkus sestavit molekulu vody z dostupných atomů:
- **Kyslík (O)**: může mít až 2 vazby
- **Vodík (H)**: může mít 1 vazbu
",
            OrderIndex = 3,
            IsActive = true
        };
    }

    /// <summary>
    /// Příklad uživatelského řešení pro obvod (správné)
    /// </summary>
    public static ValidateSolutionRequest CircuitUserSolutionCorrect()
    {
        var userSolution = new
        {
            connections = new[]
            {
                new { from = "battery-1", to = "switch-1" },
                new { from = "switch-1", to = "bulb-1" },
                new { from = "bulb-1", to = "battery-1" }
            }
        };

        return new ValidateSolutionRequest
        {
            UserSolutionJson = JsonSerializer.Serialize(userSolution)
        };
    }

    /// <summary>
    /// Příklad uživatelského řešení pro obvod (chybné)
    /// </summary>
    public static ValidateSolutionRequest CircuitUserSolutionIncorrect()
    {
        var userSolution = new
        {
            connections = new[]
            {
                new { from = "battery-1", to = "bulb-1" },
                new { from = "switch-1", to = "bulb-1" } // Chybí spojení zpět k baterii
            }
        };

        return new ValidateSolutionRequest
        {
            UserSolutionJson = JsonSerializer.Serialize(userSolution)
        };
    }

    /// <summary>
    /// Příklad uživatelského řešení pro časovou osu (správné)
    /// </summary>
    public static ValidateSolutionRequest TimelineUserSolutionCorrect()
    {
        var userSolution = new
        {
            order = new[] { "event-3", "event-1", "event-4", "event-5", "event-2" }
        };

        return new ValidateSolutionRequest
        {
            UserSolutionJson = JsonSerializer.Serialize(userSolution)
        };
    }
}
