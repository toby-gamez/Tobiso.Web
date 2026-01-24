# Interactive Exercises - Dokumentace

## Přehled

Systém interaktivních cvičení umožňuje vytvářet vzdělávací aktivity vázané na články. Podporuje různé typy cvičení včetně fyzikálních obvodů, časových os, přetahování prvků a dalších.

---

## Databázová struktura

### Entita: `InteractiveExercise`

| Sloupec | Typ | Popis |
|---------|-----|-------|
| `Id` | int | Primární klíč |
| `PostId` | int | ID článku (cizí klíč → `Posts`) |
| `Title` | string(200) | Název cvičení |
| `Type` | string(50) | Typ cvičení (circuit, timeline, drag-drop...) |
| `ConfigJson` | nvarchar(max) | JSON konfigurace cvičení |
| `SolutionJson` | nvarchar(max) | JSON se správným řešením |
| `InstructionsMarkdown` | nvarchar(max) | Volitelné instrukce (Markdown) |
| `OrderIndex` | int | Pořadí v rámci článku |
| `IsActive` | bool | Aktivní/neaktivní |
| `CreatedAt` | datetime2 | Datum vytvoření |
| `UpdatedAt` | datetime2 | Datum poslední úpravy |

**Indexy:**
- Kompozitní index: `(PostId, OrderIndex)` pro rychlé řazení cvičení

**Relace:**
- `Post` (1:N) - cvičení patří k jednomu článku, článek může mít více cvičení
- ON DELETE CASCADE - při smazání článku se smažou i cvičení

---

## API Endpointy

### Uživatelská verze (Tobiso.Web.App)

#### 1. Získat cvičení pro článek
```http
GET /api/InteractiveExercises/post/{postId}
```
- **Autorizace:** Veřejné (AllowAnonymous)
- **Vrací:** Pouze aktivní cvičení (`IsActive = true`)

#### 2. Získat konkrétní cvičení
```http
GET /api/InteractiveExercises/{id}
```
- **Autorizace:** Veřejné

#### 3. Validovat řešení
```http
POST /api/InteractiveExercises/{id}/validate
Content-Type: application/json

{
  "userSolutionJson": "{...}"
}
```
- **Autorizace:** Veřejné
- **Vrací:** `ExerciseValidationResult` se skóre a feedbackem

---

### Admin verze (Tobiso.Web.App.Admin)

#### 4. Získat všechna cvičení (včetně neaktivních)
```http
GET /api/InteractiveExercises/post/{postId}
```
- **Autorizace:** Basic Auth (Admin)

#### 5. Vytvořit nové cvičení
```http
POST /api/InteractiveExercises
Content-Type: application/json

{
  "postId": 123,
  "title": "Zapoj obvod",
  "type": "circuit",
  "configJson": "{...}",
  "solutionJson": "{...}",
  "instructionsMarkdown": "Zapoj komponenty...",
  "orderIndex": 0,
  "isActive": true
}
```
- **Autorizace:** Basic Auth (Admin)

#### 6. Aktualizovat cvičení
```http
PUT /api/InteractiveExercises/{id}
```

#### 7. Smazat cvičení
```http
DELETE /api/InteractiveExercises/{id}
```

#### 8. Získat správné řešení
```http
GET /api/InteractiveExercises/{id}/solution
```
- **Autorizace:** Pouze Admin
- **Vrací:** `SolutionJson` (nevrací se běžným uživatelům)

---

## Typy cvičení

### 1. Circuit (Fyzikální obvod)

**ConfigJson:**
```json
{
  "components": [
    {
      "id": "battery-1",
      "type": "battery",
      "voltage": 12,
      "x": 50,
      "y": 100
    },
    {
      "id": "bulb-1",
      "type": "bulb",
      "resistance": 6,
      "x": 200,
      "y": 100
    },
    {
      "id": "switch-1",
      "type": "switch",
      "state": "off",
      "x": 350,
      "y": 100
    }
  ],
  "availableComponents": [
    { "type": "bulb", "label": "Žárovka" },
    { "type": "resistor", "label": "Odpor" },
    { "type": "switch", "label": "Přepínač" }
  ]
}
```

**SolutionJson:**
```json
{
  "correctConnections": [
    { "from": "battery-1", "to": "switch-1" },
    { "from": "switch-1", "to": "bulb-1" },
    { "from": "bulb-1", "to": "battery-1" }
  ],
  "explanation": "Správně zapojený sériový obvod s přepínačem."
}
```

**User Solution (odesílá frontend):**
```json
{
  "connections": [
    { "from": "battery-1", "to": "switch-1" },
    { "from": "switch-1", "to": "bulb-1" }
  ]
}
```

---

### 2. Timeline (Časová osa)

**ConfigJson:**
```json
{
  "events": [
    { "id": "event-1", "label": "Bitva u Lipan", "year": 1434 },
    { "id": "event-2", "label": "Bitva na Bílé hoře", "year": 1620 },
    { "id": "event-3", "label": "Založení UK", "year": 1348 }
  ],
  "timeRange": { "start": 1300, "end": 1700 }
}
```

**SolutionJson:**
```json
{
  "correctOrder": ["event-3", "event-1", "event-2"],
  "explanation": "Chronologické pořadí českých historických událostí."
}
```

**User Solution:**
```json
{
  "order": ["event-3", "event-1", "event-2"]
}
```

---

### 3. Drag-Drop (Přetahování do kategorií)

**ConfigJson:**
```json
{
  "items": [
    { "id": "word-1", "text": "pes" },
    { "id": "word-2", "text": "rychlý" },
    { "id": "word-3", "text": "běží" }
  ],
  "categories": [
    { "id": "noun", "label": "Podstatné jméno" },
    { "id": "adjective", "label": "Přídavné jméno" },
    { "id": "verb", "label": "Sloveso" }
  ]
}
```

**SolutionJson:**
```json
{
  "correctPlacements": {
    "word-1": "noun",
    "word-2": "adjective",
    "word-3": "verb"
  },
  "explanation": "Správně zařazené slovní druhy."
}
```

**User Solution:**
```json
{
  "placements": {
    "word-1": "noun",
    "word-2": "adjective",
    "word-3": "verb"
  }
}
```

---

### 4. Molecule (Chemické molekuly)

**ConfigJson:**
```json
{
  "availableAtoms": [
    { "symbol": "H", "count": 4 },
    { "symbol": "C", "count": 1 },
    { "symbol": "O", "count": 2 }
  ],
  "instructions": "Sestav molekulu vody (H₂O)"
}
```

**SolutionJson:**
```json
{
  "correctAtoms": [
    { "id": "atom-1", "symbol": "H", "bonds": 1 },
    { "id": "atom-2", "symbol": "O", "bonds": 2 },
    { "id": "atom-3", "symbol": "H", "bonds": 1 }
  ],
  "bonds": [
    { "from": "atom-1", "to": "atom-2", "type": "single" },
    { "from": "atom-2", "to": "atom-3", "type": "single" }
  ]
}
```

---

## Validace řešení

Validace probíhá na backendu podle typu cvičení:

### ExerciseValidationResult
```csharp
{
  "isCorrect": true,           // Zda je odpověď 100% správná
  "score": 85,                 // Skóre 0-100
  "feedback": "Máš správně 3 z 4 spojení.",
  "explanation": "Zkontroluj zapojení baterie.",
  "detailedResults": {         // Volitelné - částečné výsledky
    "connection-1": true,
    "connection-2": false
  }
}
```

---

## Příklad použití - C# klient

```csharp
// Získání cvičení pro článek
var exercises = await httpClient.GetFromJsonAsync<List<InteractiveExerciseResponse>>(
    $"/api/InteractiveExercises/post/{postId}");

// Validace řešení
var solution = new ValidateSolutionRequest 
{ 
    UserSolutionJson = JsonSerializer.Serialize(userAnswer) 
};

var result = await httpClient.PostAsJsonAsync(
    $"/api/InteractiveExercises/{exerciseId}/validate", 
    solution);

var validation = await result.Content.ReadFromJsonAsync<ExerciseValidationResult>();

if (validation.IsCorrect)
{
    Console.WriteLine("Výborně! ✓");
}
else
{
    Console.WriteLine($"Skóre: {validation.Score}% - {validation.Feedback}");
}
```

---

## Blazor komponenty (budoucí implementace)

### Renderer pro uživatele
```razor
@page "/posts/{PostId:int}"

<h2>@post.Title</h2>
<div>@((MarkupString)post.Content)</div>

@foreach (var exercise in exercises)
{
    <InteractiveExerciseRenderer Exercise="@exercise" OnComplete="HandleComplete" />
}

@code {
    private async Task HandleComplete(ExerciseValidationResult result)
    {
        // Zobraz feedback
    }
}
```

### Admin editor
```razor
@page "/admin/exercises/{PostId:int}"

<h3>Správa cvičení - Článek @PostId</h3>

<select @bind="selectedType">
    <option value="circuit">Fyzikální obvod</option>
    <option value="timeline">Časová osa</option>
    <option value="drag-drop">Přetahování slov</option>
    <option value="molecule">Chemické molekuly</option>
</select>

@if (selectedType == "circuit")
{
    <CircuitEditor @bind-Config="config" @bind-Solution="solution" />
}

<button @onclick="SaveExercise">Uložit</button>
```

---

## Migrace

### Vytvoření migrace
```bash
dotnet ef migrations add AddInteractiveExercise \
  --project Tobiso.Web.Api \
  --startup-project Tobiso.Web.App \
  --output-dir Infrastructure/Data/Migrations
```

### Aktualizace databáze
```bash
dotnet ef database update \
  --project Tobiso.Web.Api \
  --startup-project Tobiso.Web.App
```

### Export SQL
```bash
dotnet ef migrations script \
  --project Tobiso.Web.Api \
  --startup-project Tobiso.Web.App \
  --output InteractiveExercises.sql
```

---

## Rozšíření v budoucnu

### Další typy cvičení
- **matching** - Párování prvků (např. pojem ↔ definice)
- **fill-blank** - Doplňování do textu
- **code** - Programovací úlohy s validací kódu
- **math** - Matematické rovnice s LaTeX podporou
- **map** - Geografické cvičení na mapě
- **audio** - Poslechová cvičení (např. hudební teorie)

### Statistiky
- Ukládat pokusy uživatelů (čas, počet pokusů, skóre)
- Heatmapy úspěšnosti pro optimalizaci cvičení
- Leaderboard pro motivaci

### Gamifikace
- Body za správné odpovědi
- Odznaky (badges)
- Progress tracking

---

## Bezpečnost

✅ **SolutionJson není přístupný běžným uživatelům** - pouze Admin endpoint  
✅ **Validace probíhá na backendu** - frontend nemůže podvádět  
✅ **JSON validace** - kontrola před uložením do databáze  
✅ **Cascade delete** - při smazání článku se smažou cvičení  
✅ **Authorization** - Admin operace vyžadují Basic Auth  

---

## Kontakt

Pokud máte dotazy nebo návrhy na vylepšení, kontaktujte vývojový tým.
