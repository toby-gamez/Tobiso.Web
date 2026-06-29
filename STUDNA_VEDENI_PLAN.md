# Studna vědění – Plán redesignu Tobiso

> Cíl: Přeměnit Tobiso z kolekce stránek na propojenou síť znalostí – každý článek je uzel, který přirozeně vede k dalšímu obsahu napříč předměty.

---

## Přehled fází

| Fáze | Název | Náročnost | Stav |
|------|-------|-----------|------|
| 1 | Article Context Panel (split layout) | Nízká | ✅ Hotovo |
| 2 | Graf znalostí | Střední | 🔲 Nezačato |
| 3 | GeoContext – Mapa | Střední | 🔲 Nezačato |
| 4 | HistoryContext – Časová osa | Nízká | 🔲 Nezačato |
| 5 | Explorační mód | Vysoká | 🔲 Nezačato |
| 6 | AI-Generated Interactive Exercises | Střední | 🔲 Nezačato |
| 7 | Concept Map (myšlenková mapa) | Nízká | 🔲 Nezačato |
| 8 | Formula Playground | Střední | 🔲 Nezačato |
| 9 | Cross-subject Connector | Střední | 🔲 Nezačato |
| 10 | Learning Path & Progress | Vysoká | 🔲 Nezačato |

---

## Tasks

| Fáze | Název | Status |
|------|-------|--------|
| 0 | Studentské účty (local + Google OAuth, kredity, AI chat historie) | ✅ Done |
| 1 | Context Panel (split layout) | ✅ Done |
| 2 | Knowledge Graph (AI tags, DB changes) | 🔲 Not started |
| 3 | GeoContext / Map (Leaflet.js, DB) | 🔲 Not started |
| 4 | History Timeline (DB, timeline strip) | 🔲 Not started |
| 5 | Explore page (/explore, D3 full graph) | 🔲 Not started |
| 6 | AI-Generated Interactive Exercises | 🔲 Not started |
| 7 | Concept Map | 🔲 Not started |
| 8 | Formula Playground | 🔲 Not started |
| 9 | Cross-subject Connector | 🔲 Not started |
| 10 | Learning Path & Progress | 🔲 Not started |
| 11 | "Proč?" explainer | 🔲 Not started |
| 12 | Definition tooltips | 🔲 Not started |
| 13 | Reading progress & bookmarks | 🔲 Not started |
| 14 | Exam predictor | 🔲 Not started |
| 15 | Surprise facts sidebar card | 🔲 Not started |
| 16 | Socratic tutor mode | 🔲 Not started |
| 17 | Difficulty rewrite (register switch) | 🔲 Not started |
| 18 | Personal notes | 🔲 Not started |
| 19 | Study timer (Pomodoro) | 🔲 Not started |
| 20 | Reading streak & subject badges | 🔲 Not started |
| 21 | Difficulty rating by students | 🔲 Not started |
| 22 | Random article / Article of the day | 🔲 Not started |
| 23 | Comparison tables (AI generated) | 🔲 Not started |
| 24 | Step-by-step solver | 🔲 Not started |
| 25 | Video context card | 🔲 Not started |
| 26 | Teacher assignments & confusion heatmap | 🔲 Not started – requires accounts |
| 1b | Exercises in sidebar (layout) | ✅ Done |
| 27 | Spaced Repetition & Téma dne | 🔲 Not started – requires accounts |
| 28 | Dějepis jako živá timeline | 🔲 Not started – 28b blokováno dostupností GeoJSON dat |

> **Note:** Fáze 1 contains only the split layout and buttons linking to existing modals. The actual Map, Timeline, and AI-tag Graph content for those buttons is built in Fáze 2–4.

---

## Roadmap – pořadí implementace

```
BLOK 1 – Základ (prerekvizita pro vše s identitou)
  └─ Fáze 0   Uživatelské účty, AI chat historie, kreditový systém        6–8 dní

BLOK 2 – Rychlé výhry (nezávislé, žádné prerekvizity)
  ├─ Fáze 22  Náhodný článek / Článek dne                                  1 den
  ├─ Fáze 15  Surprise facts sidebar ("Věděl jsi, že…?")                   1 den
  ├─ Fáze 14  Exam predictor ("Co bude v testu?")                          1–2 dny
  ├─ Fáze 21  Difficulty rating (😊/😐/😕 po přečtení)                      1 den
  ├─ Fáze 17  Difficulty rewrite (8letý / gymnázium / odborník)            1 den
  ├─ Fáze 19  Study timer (Pomodoro)                                       1 den
  └─ Fáze 25  Video context card (YouTube embed)                           1 den

BLOK 3 – Osobní funkce (využívají účty z Bloku 1)
  ├─ Fáze 13  Reading progress & záložky (scroll %, bookmarks v DB)        1–2 dny
  ├─ Fáze 18  Personal notes (per-článek poznámky)                         1 den
  └─ Fáze 20  Reading streak & subject badges                              2–3 dny

BLOK 4 – AI obsah (na sobě nezávislé, využívají AiService)
  ├─ Fáze 11  "Proč?" explainer (kauzální AI vysvětlení věty)              1–2 dny
  ├─ Fáze 12  Definition tooltips (hover definice klíčových pojmů)         2–3 dny
  ├─ Fáze 16  Socratic tutor mode (AI pokládá otázky místo odpovědí)       2–3 dny
  ├─ Fáze 23  Comparison tables ("Porovnat s…")                            2 dny
  ├─ Fáze 24  Step-by-step solver (krok za krokem přes příklady)           2–3 dny
  ├─ Fáze 6   AI Interactive Demo (GPT generuje HTML/JS widget do iframe)  2–3 dny
  ├─ Fáze 7   Concept Map (myšlenková mapa pojmů uvnitř článku)            3–4 dny
  ├─ Fáze 8   Formula Playground (KaTeX vzorce + slidery proměnných)       3–4 dny
  └─ Fáze 9   Cross-subject Connector (osmóza → fyzika → chemie)           2–3 dny

BLOK 5 – Grafy a navigace
  ├─ Fáze 2   Knowledge Graph (AI tagy, D3 orbit kolem článku)             8–10 dní
  └─ Fáze 5   Explore page (/explore, D3 celý graf)           [po Fázi 2]  5–6 dní

BLOK 6 – Mapa a časová osa
  ├─ Fáze 3   GeoContext – Leaflet mapa v článku                           4–5 dní
  ├─ Fáze 4   History Timeline strip v článku                              3–4 dny
  ├─ Fáze 28  Dějepis jako živá timeline (/history)          [po Fázi 4]   6–8 dní
  └─ Fáze 28b Živá historická mapa (scrubber + polygony)  [po Fázi 28+3]  10–15 dní
                ⚠️ blokováno dostupností GeoJSON dat

BLOK 7 – Pokročilé osobní funkce (vyžadují účty + data z předchozích bloků)
  ├─ Fáze 10  Learning Path & Progress                       [po Bloku 1]  4–5 dní
  ├─ Fáze 26  Teacher tools (zadávání, confusion heatmap)    [po Bloku 1]  5–7 dní
  └─ Fáze 27  Spaced Repetition & Téma dne (SM-2 algoritmus)[po Fázi 10]  10–14 dní
```

**Celkový čas:** ~80–105 dní (bez Fáze 28b která závisí na datech)

**Doporučené pořadí bloků:** 1 → 2 → 3 → 4 → 5 → 6 → 7

> Bloky 2, 3, 4 lze dělat paralelně nebo libovolně prohazovat. Blok 5 závisí sám na sobě (Fáze 5 po Fázi 2). Blok 6 závisí sám na sobě (28 po 4, 28b po 28 a 3). Blok 7 závisí na Bloku 1.

> **Tip – quick wins:** Před zahájením Fáze 2 (8–10 dní) zvažte udělat nejdřív pár rychlých fází pro viditelný pokrok: **Fáze 15** (fun facts, 1 den), **Fáze 14** (exam predictor, 1–2 dny), **Fáze 22** (náhodný článek, 1 den). Jsou nezávislé, neblokují nic a dají uživatelům něco nového rychle.

---

## Fáze 1 – Article Context Panel (split layout) ✅

**Co to je:** Článek rozdělený napůl – vlevo obsah, vpravo sticky kontextový panel s navigací a doplňkovými informacemi.

**Implementováno (28. 6. 2026):**
```
┌─────────────────────┬─────────────────────┐
│                     │ [Obsah – TOC]       │
│   ČLÁNEK (text)     │ [Graf znalostí]     │
│                     │ [Co kdyby?]         │
│   lorem ipsum...    │ [Reálné využití]    │
│                     │ [Zkus to vysvětlit] │
│                     │ [Mapa] (brzy)       │
│                     │ [Časová osa] (brzy) │
│                     │ [Obrázky z článku]  │
│                     │ [Související]        │
└─────────────────────┴─────────────────────┘
```

**Implementované soubory:**
- `PostDetail.razor` – split layout `.article-split-layout` + `<aside class="article-context-sidebar">`
- `wwwroot/css/style.css` – `.article-split-layout`, `.context-card`, `.context-feature-btn`, `.sidebar-*`

**Chování:**
- Desktop (>1100px): 50/50 grid, sidebar sticky
- Mobile (<1100px): jeden sloupec, sidebar nahoře (TOC skrytý)
- Focus mode: sidebar schovaný
- TOC přesunutý z fixní pozice do sidebar cardu
- Obrázky extrahovány z markdown regexem, zobrazeny jako grid
- Related posts přesunuty ze spodku do sidebar
- Hotové modaly přístupné přímo ze sidebar (Graf, Co kdyby?, Reálné využití, Feynman)

---

## Fáze 2 – Graf znalostí (Knowledge Graph)

**Co to je:** Automatické propojení článků pomocí AI-generovaných tagů. Rozšíření stávajícího `PostsGraphModal.razor`.

### Databázové změny

```sql
-- Nová tabulka pro AI tagy
CREATE TABLE PostTags (
    Id INT PRIMARY KEY,
    PostId INT NOT NULL,
    Tag NVARCHAR(100) NOT NULL,
    Source NVARCHAR(20) NOT NULL  -- 'ai' | 'manual'
);

-- Index pro rychlé hledání sdílených tagů
CREATE INDEX IX_PostTags_Tag ON PostTags(Tag);
```

### Logika propojení

```
Typ vazby:
  silná  = explicitní RelatedPost (plná čára)
  střední = sdílený AI tag (tečkovaná čára)
  slabá  = stejná kategorie (šedá čára)
```

### Rozšíření UI

**Mini orbit** – malý inline graf kolem aktuálního článku přímo na PostDetail stránce (ne jen modal):
- Radius 3 stupně od aktuálního uzlu
- Klik na sousední uzel = přechod na článek
- Nahrazuje statický seznam "Doporučené články"

**Nové soubory:**
- `Tobiso.Web.Domain/Entities/PostTag.cs`
- `Tobiso.Web.Api/Services/PostTagService.cs`
- `Tobiso.Web.App/Components/Shared/ArticleOrbit.razor`

**Změny stávajících souborů:**
- `PostsGraphModal.razor` – přidání tagových vazeb, barevné rozlišení typů vazeb
- `PostDetail.razor` – přidání `<ArticleOrbit>` místo statického "related posts"
- `AiService.cs` – metoda `GenerateTagsForPost(postId)`

**Odhadovaná práce:** 8–10 dní

> ⚠️ **Pozor na time estimate:** Původní odhad 5–7 dní podceňoval D3 orbit UI. AI tag generation + ArticleOrbit component + graph extensions + migration = reálně 8–10 dní při potřebě vizuálně vyladit orbit.

---

## Fáze 3 – GeoContext (Mapa)

**Co to je:** Leaflet.js mapa zobrazující se přímo v článku pro geograficky relevantní obsah.

### Databázové změny

```sql
CREATE TABLE PostGeoTags (
    Id INT PRIMARY KEY,
    PostId INT NOT NULL,
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6),
    ZoomLevel INT DEFAULT 6,
    Label NVARCHAR(200),
    Type NVARCHAR(20)  -- 'point' | 'region' | 'route'
);
```

### Využití v předmětech

| Předmět | Příklad |
|---------|---------|
| Zeměpis | Kraj/stát/reliéf → zobrazí bod/region na mapě |
| Dějepis | Karel IV. → Praha, Karlštejn, hranice Českého království |
| Přírodopis | Habitat živočicha → rozšíření na mapě |

### UI

- Panel se rozbalí z Contextual Layer (Fáze 1)
- Leaflet.js mapa s OpenStreetMap tiles
- Historické mapy jako overlay (např. Přemyslovské Čechy)

**Nové soubory:**
- `Tobiso.Web.Domain/Entities/PostGeoTag.cs`
- `Tobiso.Web.App/Components/Shared/GeoMapPanel.razor`
- `Tobiso.Web.App/wwwroot/js/geo-map.js`

**Admin rozšíření:**
- `EditPost.razor` – přidat záložku "Geodata" s interaktivním výběrem bodu na mapě

**Odhadovaná práce:** 4–5 dní

---

## Fáze 4 – HistoryContext (Časová osa)

**Co to je:** Mini timeline strip na PostDetail stránce propojující článek s historickými událostmi.

### Databázové změny

Rozšíření existujícího `Event` modelu:
```sql
ALTER TABLE Events ADD Era NVARCHAR(50);  -- 'pravěk', 'středověk', 'novověk', ...

CREATE TABLE EventPostLinks (
    EventId INT NOT NULL,
    PostId INT NOT NULL
);
```

### UI

```
◄─── starověk ────── středověk ──────── novověk ────►
              |                |
         [Karel IV.]     [Husité]
              ↑ (aktuální článek)
```

- Zobrazuje 5 událostí vlevo/vpravo od aktuálního článku
- Klik na událost = přechod na propojený článek
- Propojení s existujícím `Calendar.razor` (sdílená data)

**Nové soubory:**
- `Tobiso.Web.App/Components/Shared/HistoryTimelineStrip.razor`

**Změny stávajících souborů:**
- `Tobiso.Web.Domain/Entities/Event.cs` – přidání `Era`, `LinkedPostIds`
- `PostDetail.razor` – přidání `<HistoryTimelineStrip>` pro dějepis

**Odhadovaná práce:** 3–4 dny

---

## Fáze 5 – Explorační mód

**Co to je:** Nová vstupní stránka `/explore` – vizuální mapa celé studny vědění místo textového menu.

### UI koncepty

**Graf mód:** Plnoobrazovkový D3.js graf 350 článků
- Barevné shluky podle předmětu
- Filtr podle předmětu, ročníku, tématu
- Klik na uzel = otevře article preview panel (bez opuštění grafu)
- Dvojklik = přechod na plný článek

**Mapa mód:** Zeměpisná mapa s piny článků
- Přepnutí z grafu na mapu jedním kliknutím
- Clustery článků ze stejné oblasti

**Osa mód:** Horizontální timeline všech historických událostí
- Zoom na období
- Klik = article preview

**Nové soubory:**
- `Tobiso.Web.App/Components/Pages/Explore.razor`
- `Tobiso.Web.App/wwwroot/js/explore-graph.js` (rozšíření posts-graph.js)

**Nové API endpointy:**
- `GET /api/explore/graph` – optimalizovaná data pro celý graf (jen id, title, categoryId, tags)
- `GET /api/explore/geo` – všechny geolokace článků

**Odhadovaná práce:** 5–6 dní

---

## Technologický stack (nový)

| Knihovna | Účel | Stav |
|----------|------|------|
| D3.js v7 | Graf znalostí | ✅ Existuje |
| Leaflet.js | Mapy | 🔲 Přidat |
| OpenStreetMap | Map tiles (zdarma) | 🔲 Přidat |

---

## Celkový odhadovaný čas

| Fáze | Dny |
|------|-----|
| 1 – Contextual Layer | 3–4 |
| 2 – Graf znalostí | 8–10 |
| 3 – Mapa | 4–5 |
| 4 – Časová osa | 3–4 |
| 5 – Explorační mód | 5–6 |
| **Celkem** | **23–29 dní** |

---

## Příklad: Karel IV. jako dokonalý uzel

Po implementaci všech fází bude článek o Karlu IV. vypadat takto:

```
┌─────────────────────────────────────────────────────────────────┐
│ DĚJEPIS › Středověk › Karel IV.                                  │
│ [📍 Praha, Čechy]  [🕐 1316–1378]  [👤 Karel IV.]  [🔗 12 uzlů] │
└─────────────────────────────────────────────────────────────────┘

# Karel IV.

... obsah článku ...

┌─ Mapa ──────────────────────┐  ┌─ Síť uzlů ─────────────────┐
│  [Leaflet mapa Čech 1350]   │  │  Karlštejn → Gotická arch. │
│  • Praha  • Karlštejn       │  │  Praha → Zeměpis: Čechy    │
│  • Norimberk                │  │  Universita → Literatura   │
└─────────────────────────────┘  └────────────────────────────┘

◄── Přemyslovci ──── [Karel IV.] ──── Husité ──►
         │                                │
    [Gotická architektura]         [Jan Hus]
```

---

## Fáze 6 – AI-Generated Interactive Exercises

**Co to je:** GPT-4o dostane obsah článku a vygeneruje self-contained HTML/CSS/JS interaktivní widget, který se zobrazí v sandboxovaném `<iframe>`. Žádné předem definované šablony – AI rozhodne, co je nejlepší vizualizace pro dané téma.

**Jak to funguje:**
1. Uživatel klikne "Vygenerovat interaktivní demo"
2. Server pošle GPT-4o: obsah článku + instrukci
3. GPT-4o vrátí kompletní HTML soubor (Canvas/SVG + vanilla JS)
4. Uložíme do DB jako `type = "ai-html"`, `HtmlContent = "..."`
5. Zobrazíme v `<iframe srcdoc="..." sandbox="allow-scripts">`

**Příklady co AI dokáže vygenerovat:**
- **Zákon akce a reakce**: dva objekty s animovanými šipkami sil, posuvník hmotnosti
- **Ohmův zákon**: interaktivní obvod, sliders R/U, živý výpočet I
- **Historická mapa**: SVG mapa s animovaným šířením říší (Karel IV. → rozšíření území)
- **Fotosyntéza**: animace vstupu/výstupu molekul v buňce
- **Pythagorova věta**: drag&drop trojúhelník, vizuální proof

**Prompt šablona pro GPT-4o:**
```
Vytvoř interaktivní HTML/JS demonstraci konceptu z tohoto článku.
Použij Canvas nebo SVG + vanilla JS, žádné externí knihovny.
Výstup: kompletní HTML soubor (<!DOCTYPE html>...) který funguje samostatně.
Styl: čistý, moderní, vhodný pro studenty ZŠ/SŠ.
Animace by měly být responzivní na vstup uživatele (sliders, klikání, drag).
Max. 200 řádků kódu.

Článek:
{content}
```

**Nové soubory:**
- `Tobiso.Web.App/Components/AiInteractiveDemo.razor` – tlačítko + iframe zobrazení
- `Tobiso.Web.App/Controllers/AiController.cs` – endpoint `POST /api/ai/generate-demo/{postId}`
- `Tobiso.Web.Api/Services/AiService.cs` – metoda `GenerateInteractiveDemoAsync(postContent)`

**DB:** Uložit jako existující `InteractiveExercise` s `Type = "ai-html"` + nový sloupec `HtmlContent TEXT`

**Odhadovaná práce:** 2–3 dny

---

## Fáze 7 – Concept Map (Myšlenková mapa)

**Co to je:** AI vygeneruje vizuální myšlenkovou mapu pojmů UVNITŘ jednoho článku (ne mezi články – to je Fáze 2). Zobrazí se jako collapsible panel v context sidebaru.

**Jak funguje:**
- GPT-4o extrahuje 6–12 klíčových pojmů + jejich vztahy z článku
- Vrátí JSON: `{ nodes: [...], edges: [...] }`
- Vykreslit minimalistickým D3.js force layoutem (rozšíření posts-graph.js)
- Klik na pojem → scroll na místo v článku kde se vyskytuje

**Příklad výstupu (Zákon akce a reakce):**
```
[Síla] ←→ [Reakce]
   ↓           ↓
[Hmotnost]  [Třetí Newtonův zákon]
   ↓
[Zrychlení (F=ma)]
```

**Nové soubory:**
- Rozšíření `ArticleContextPanel` o `.concept-map-card`
- `Tobiso.Web.App/wwwroot/js/concept-map.js`

**Odhadovaná práce:** 3–4 dny

---

## Fáze 8 – Formula Playground

**Co to je:** Fyzikální/matematické vzorce v KaTeX se stanou interaktivními. Uživatel může táhnout slidery proměnných a vidět výsledek v reálném čase.

**Jak funguje:**
- Parser hledá v markdown vzorce jako `$F = m \cdot a$`
- GPT-4o identifikuje proměnné a rozsahy hodnot
- JS přidá pod vzorec sadu sliderů
- Výpočet živě aktualizuje výsledek

**Příklad:**
```
F = m · a

m [slider: 1–100 kg] = 10 kg
a [slider: 0–50 m/s²] = 9.8 m/s²
─────────────────────────────
F = 98 N  ← živě se mění
```

**Předměty:** fyzika (F=ma, U=RI, E=mc²), chemie (PV=nRT), matematika

**Odhadovaná práce:** 3–4 dny

---

## Fáze 9 – Cross-subject Connector

**Co to je:** AI automaticky hledá konceptuální propojení mezi předměty. Příklad: osmóza v biologii → tlak ve fyzice → rovnováha v chemii.

**UI:** Malý card v context sidebaru: "Tento princip v jiných předmětech:"
- Klik → přejde na příslušný článek

**Jak funguje:**
- Při načtení článku → API call: `GET /api/ai/cross-connections/{postId}`
- GPT-4o dostane obsah článku + seznam titulů všech článků
- Vrátí max. 3 doporučení s krátkým vysvětlením proč jsou relevantní
- Výsledek cache-ovat v DB (nová tabulka `PostCrossConnections`)
- **Cache invalidace:** Smazat záznamy při update obsahu článku — AI doporučení jsou vázaná na konkrétní obsah.

**Příklad výstupu (Zákon akce a reakce):**
```
Tento princip se objevuje i v:
→ Biologie: Srdeční sval (kontrakce = akce, krev = reakce)
→ Chemie: Oxidace-redukce (donor = akce, akceptor = reakce)  
→ Zeměpis: Tektonické desky (tlak = akce, zemětřesení = reakce)
```

**Odhadovaná práce:** 2–3 dny

---

## Fáze 10 – Learning Path & Progress

**Co to je:** Osobní učební cesta. Tobiso si pamatuje, které články student přečetl, jak si vedl v kvízech, a navrhuje co studovat dál.

**Funkce:**
- Progress bar uvnitř článku (% přečteno, uloženo do localStorage)
- "Přečteno" badge na kartičkách v kategorii
- Navrhovaný další článek na konci (AI rozhoduje podle kategorie + historie)
- Celková statistika: "Přečetl jsi 12 článků z Fyziky, zbývá 8"

**UI:**
```
[██████████░░░░░░░░░░] 58% přečteno

Doporučuji dál: "Druhý Newtonův zákon →"
```

**Implementace:**
- localStorage pro anonymní uživatele
- Server-side pro přihlášené (nová tabulka `UserProgress`)
- `GET /api/ai/suggest-next/{postId}` – AI navrhuje co dál

**Odhadovaná práce:** 4–5 dní

---

## Celkový odhadovaný čas (aktualizováno)

| Fáze | Dny |
|------|-----|
| 1 – Context Panel | ✅ 2 dny |
| 2 – Graf znalostí | 8–10 |
| 3 – Mapa | 4–5 |
| 4 – Časová osa | 3–4 |
| 5 – Explorační mód | 5–6 |
| 6 – AI Interactive Exercises | 2–3 |
| 7 – Concept Map | 3–4 |
| 8 – Formula Playground | 3–4 |
| 9 – Cross-subject Connector | 2–3 |
| 10 – Learning Path | 4–5 |
| 11 – "Proč?" explainer | 1–2 |
| 12 – Definition tooltips | 2–3 |
| 13 – Reading progress & bookmarks | 1–2 |
| 14 – Exam predictor | 1–2 |
| 15 – Surprise facts | 1 |
| 16 – Socratic tutor | 2–3 |
| 17 – Difficulty rewrite | 1 |
| 18 – Personal notes | 1 |
| 19 – Study timer | 1 |
| 20 – Streak & badges | 2–3 |
| 21 – Difficulty rating | 1 |
| 22 – Random / Article of the day | 1 |
| 23 – Comparison tables | 2 |
| 24 – Step-by-step solver | 2–3 |
| 25 – Video context card | 1 |
| 26 – Teacher tools | 5–7 |
| **Celkem** | **51–68 dní** |

---

## Fáze 11 – "Proč?" explainer

**Co to je:** Klik na libovolnou větu → AI vysvětlí PROČ je to pravda, ne jen CO to znamená. Doplněk k existujícímu ExplainSentence (který vysvětluje WHAT).

**Příklad:** Věta "Těleso se pohybuje rovnoměrně přímočaře, pokud na něj nepůsobí žádná síla." → "Proč?" → AI vysvětlí inertii přes analogii: loď na klidném moři, absence tření ve vesmíru, atd.

**Implementace:** Nový endpoint `POST /api/ai/why` (podobný `/explain-sentence`), jiný system prompt zaměřený na kauzalitu. UI: druhá ikona v sentence helper tooltipu (vedle existující žárovky).

**Odhadovaná práce:** 1–2 dny

---

## Fáze 12 – Definition tooltips

**Co to je:** AI jednou projde článek a označí 8–12 klíčových termínů. Na hover se zobrazí 1-větná definice bez kliknutí.

**Jak funguje:**
- `GET /api/ai/key-terms/{postId}` → vrátí `[{ term, definition }]`
- Výsledek cache-ovat v DB (nová tabulka `PostKeyTerms`)
- JS post-processing v `MarkdownContent`: wrap každý term do `<span class="key-term" data-def="...">`, tooltip přes CSS/JS
- **Cache invalidace:** Pokud se změní obsah článku (nová `PostVersion`), smazat záznamy z `PostKeyTerms` pro daný `PostId` — jinak zůstanou definice pro staré termíny.

**Rozdíl od PersonModal:** PersonModal je pro historické osoby. Toto je pro fyzikální/chemické/geografické termíny.

**Odhadovaná práce:** 2–3 dny

---

## Fáze 13 – Reading progress & bookmarks

**Co to je:** Scroll position uložena do localStorage → progress bar nahoře v článku. Bookmarks: hvězdička na kartičce, seznam na `/bookmarks`.

**Vše lokální (localStorage), žádný backend:**
- `readProgress_{postId}` = 0–100
- `bookmarks` = `[postId, ...]`
- Progress bar: tenká linka pod tools barem, aktualizuje se při scrollu

**Odhadovaná práce:** 1–2 dny

---

## Fáze 14 – Exam predictor

**Co to je:** Tlačítko "Co bude v testu?" → AI vygeneruje 5 nejpravděpodobnějších zkouškových otázek z článku, s krátkými vzornými odpověďmi.

**Implementace:** Nový endpoint `POST /api/ai/exam-questions/{postId}`, výsledek zobrazit v modalu (podobný PracticeProblems, ale zaměřený na předpověď, ne generické procvičování).

**Odhadovaná práce:** 1–2 dny

---

## Fáze 15 – Surprise facts

**Co to je:** Sidebar card "Věděl jsi, že…?" se 3 AI-generovanými překvapivými fakty o tématu článku. Zobrazí se pod context shortcuts.

**Implementace:** `GET /api/ai/fun-facts/{postId}`, cache v DB, zobrazit jako bulleted list v context cardu. Collapsible (výchozí skrytý, klik = rozbalí).
- **Cache invalidace:** Mazat cached fakty při update článku (stejný princip jako u Fáze 12 a 9).

**Odhadovaná práce:** 1 den

---

## Fáze 16 – Socratic tutor mode

**Co to je:** AI místo přímých odpovědí pokládá naváděcí otázky. Alternativní mód AiChatBox přepínatelný tlačítkem "Sokratovský mód".

**Prompt změna:** System prompt dostane instrukci: "Nikdy neodpovídej přímo. Vždy se zeptej zpět naváděcí otázkou, která přivede studenta k odpovědi."

**Odhadovaná práce:** 2–3 dny

---

## Fáze 17 – Difficulty rewrite

**Co to je:** Rozšíření existujícího GradeRewriteru o explicitní registry: "Vysvětli jako bych byl 8letý", "Vysvětli jako studentovi gymnázia", "Vysvětli jako odborníkovi". Bez výběru ročníku.

**Odhadovaná práce:** 1 den

---

## Fáze 18 – Personal notes

**Co to je:** Malé textové pole per článek, uložené do localStorage. Přístupné přes novou ikonu v tools baru nebo přes sidebar card.

**Odhadovaná práce:** 1 den

---

## Fáze 19 – Study timer

**Co to je:** Pomodoro timer v reading panelu: 25min čtení + 5min pauza. Vizuální odpočítávání, notifikace přes Web Notifications API.

**Odhadovaná práce:** 1 den

---

## Fáze 20 – Reading streak & subject badges

**Co to je:** localStorage sleduje dny v řadě s alespoň jedním přečteným článkem. Po N článcích z předmětu student získá badge (Fyzikář I/II/III, Historik, atd.). Zobrazeno na `/profile` nebo v navbar.

**Odhadovaná práce:** 2–3 dny

---

## Fáze 21 – Difficulty rating

**Co to je:** Po dočtení článku (>80% scroll) se zobrazí nenápadný widget: "Jak ti přišel článek? 😊 Snadný / 😐 OK / 😕 Těžký". Odpovědi ukládají do DB, agregát slouží k řazení doporučení.

**Odhadovaná práce:** 1 den

---

## Fáze 22 – Random article / Article of the day

**Co to je:** Tlačítko "Náhodný článek" v navigation menu. "Článek dne" na homepage — rotuje denně, vybrán podle toho, co je v databázi nejméně čtené.

**Odhadovaná práce:** 1 den

---

## Fáze 23 – Comparison tables

**Co to je:** Tlačítko v tools menu "Porovnat s…" → uživatel zadá druhý pojem → AI vygeneruje přehlednou srovnávací tabulku.

**Příklady:** Románský vs. Gotický styl, Rostlinná vs. Živočišná buňka, Newton vs. Einstein, Fotosyntéza vs. Dýchání.

**Odhadovaná práce:** 2 dny

---

## Fáze 24 – Step-by-step solver

**Co to je:** Pro články s příklady (fyzika, matematika, chemie) — tlačítko "Vyřeš krok za krokem" → AI projde příklad z článku a ukáže postup s vysvětlením každého kroku.

**Odhadovaná práce:** 2–3 dny

---

## Fáze 25 – Video context card

**Co to je:** Admin může přiřadit YouTube URL + timestamp k článku. V context sidebaru se zobrazí card "Video" s náhledem a tlačítkem přehrát (otevře modal s embedded playerem).

**Odhadovaná práce:** 1 den

---

## Fáze 26 – Teacher tools

> ⚠️ **Vyžaduje uživatelské účty.** Učitelé i studenti musí být přihlášeni — bez auth nelze přiřazovat úkoly ani sledovat kdo co splnil. Implementace accounts je prerekvizita (viz Fáze 27).

**Co to je:** Učitel může přiřadit články třídě jako domácí úkol s termínem. Studenti označí jako přečteno. Učitel vidí dashboard s přehledem.

**Funkce:**
- Assignments: `POST /api/assignments` → přiřadit článek třídě + deadline
- Student vidí badge "Zadáno do X.X." na kartičce článku
- Student označí jako přečteno → `POST /api/assignments/{id}/complete`
- Confusion heatmap: student může kliknout na paragraf a označit "Nechápu" → učitel vidí agregovanou mapu zmatení po paragrafech

**Nové entity:** `Assignment`, `AssignmentCompletion`, `ParagraphConfusion`

**Odhadovaná práce:** 5–7 dní

---

## Fáze 27 – Spaced Repetition & Téma dne

**Co to je:** Osobní denní procvičování — algoritmus rozhodne, který článek student potřebuje dnes zopakovat na základě toho, jak dávno ho četl a jak mu šel kvíz. Každý den jedno nové téma + opakování starých.

> ⚠️ **Vyžaduje uživatelské účty.** Bez přihlášení nelze personalizovat — data by se ztratila při vymazání cache. Implementace accounts je prerekvizita (viz níže).

**Algoritmus (SM-2 zjednodušený):**
1. Student přečte článek → ohodnotí pochopení: 😊 Dobře / 😐 Tak tak / 😕 Špatně
2. Hodnocení určí interval do dalšího opakování: 1 / 3 / 7 / 14 / 30 dní
3. Každý den homepage zobrazí seznam článků k opakování na dnes
4. Nové témata se dávkují: 1–2 nové za den, zbytek jsou opakování

**UI:**
```
Dnes:
  🔁 K opakování (3):  Zákon akce a reakce  |  Karel IV.  |  Fotosyntéza
  🆕 Nové téma:        Archimédův zákon  →

[Streak: 🔥 7 dní v řadě]  [Celkem přečteno: 34 článků]
```

- `/dashboard` — osobní přehled, kalendář opakování, progress po předmětech
- Badge "⏰ Dnes" na article kartičkách v kategoriích
- Po přečtení článku: lišta se sebehodnocením (1–3 nebo emoji)

**Prerekvizita — Uživatelské účty:**
- Registrace / přihlášení (email + heslo, nebo Google OAuth)
- Stávající Basic Auth je admin-only → zůstane beze změn
- Nový cookie-based auth pro studenty (ASP.NET Core Identity nebo custom)
- Nové tabulky: `Users`, `UserArticleProgress`, `ReviewSchedule`

**Nové soubory:**
- `Tobiso.Web.Domain/Entities/User.cs`, `UserArticleProgress.cs`
- `Tobiso.Web.Api/Services/SpacedRepetitionService.cs`
- `Tobiso.Web.App/Components/Pages/Dashboard.razor`
- Controllers: `POST /api/progress/rate`, `GET /api/progress/today`

**Odhadovaná práce:** 10–14 dní (včetně accounts systému)

---

## Fáze 28 – Dějepis jako živá timeline

> ⭐ **Dějepis je samostatný předmět se speciálním pohledem.** Místo obyčejné kategorie se seznamem článků (jako Fyzika, Chemie atd.) dostane dějepis vlastní vstupní stránku `/history` — interaktivní časovou osu. Odkaz "Dějepis" v navigaci povede přímo na tuto osu, ne na grid článků. Je to záměrné odlišení: dějepis je ze své podstaty chronologický, takže timeline je přirozenější vstupní bod než kartičky.

**Co to je:** Interaktivní časová osa kde každá historická událost je bod, klik otevře popup s obsahem článku. Osa je filtrovatelná podle geografické oblasti — Celý svět, Evropa, Čechy, Balkán / Albánie, Středomoří, Asie, Amerika.

> Fáze 4 (HistoryContext strip) zůstává jako doplněk uvnitř článku. Toto je samostatný vstupní bod pro celý dějepis.

**Vizuální návrh:**

```
/history

Filtr oblasti: [Celý svět ▾]  [Evropa ▾]  [Čechy ▾]  [Balkán / Albánie ▾]  [Středomoří ▾]  [Asie ▾]  [Amerika ▾]

──────────────────────────────────────────────────────────────────►
 500    800   1000   1200   1400   1600   1800   2000
         │           │      │
    [Karel Vel.]  [Křížové │  ]
                  výpravy  │
                        [Karel IV.]  ●  ←── hover/klik
                                     │
                              ┌──────┴──────────────┐
                              │ Karel IV.            │
                              │ 1316–1378            │
                              │                      │
                              │ [obsah článku...]    │
                              │                      │
                              │ [Otevřít plný článek]│
                              └──────────────────────┘
```

**Klíčové vlastnosti:**
- **Osa** je primární navigace pro dějepis (nahrazuje category grid)
- **Zoom**: scroll = přiblížení/oddálení časového úseku (jako Google Maps)
- **Filtry geografické oblasti**: Čechy, Evropa, Středomoří, Asie, Amerika, Svět — přepínatelné, kombinovatelné
- **Popup**: klik na událost otevře mini-verzi článku přímo v popupu (MarkdownContent), bez opuštění timeline
- **Barvy**: každá geografická oblast má svou barvu bodů
- **Přesah**: jedna událost může patřit do více oblastí (křížové výpravy = Evropa + Středomoří)
- **Éry**: vizuální pruhy pod osou — Pravěk / Starověk / Středověk / Novověk / Souč.

**Databázové změny:**

```sql
-- Rozšíření existující tabulky Events
ALTER TABLE Events ADD LinkedPostId INT;             -- přímý odkaz na Post
ALTER TABLE Events ADD Era NVARCHAR(50);             -- 'starověk', 'středověk', ...
ALTER TABLE Events ADD EndYear INT;                  -- pro události s trváním
ALTER TABLE Events ADD Importance INT DEFAULT 1;     -- 1–3, ovlivňuje velikost bodu

-- Join tabulka pro oblasti (místo GeoAreas NVARCHAR(500) s JSON)
-- Důvod: JSON array v NVARCHAR je nefiltratelný bez parsování; join tabulka umožňuje
-- WHERE EXISTS (SELECT 1 FROM EventGeoAreas WHERE EventId = e.Id AND Area = 'cechy')
CREATE TABLE EventGeoAreas (
    EventId INT NOT NULL,
    Area    NVARCHAR(100) NOT NULL,  -- 'cechy', 'evropa', 'svet', ...
    PRIMARY KEY (EventId, Area)
);
```

> ⚠️ **Schema poznámka:** Původní návrh měl `GeoAreas NVARCHAR(500)` jako JSON array. To bylo odstraněno ve prospěch `EventGeoAreas` join tabulky — filtry podle oblasti pak jdou dělat přes SQL bez parsování JSONu.

**Nové soubory:**
- `Tobiso.Web.App/Components/Pages/HistoryTimeline.razor` — stránka `/history`
- `Tobiso.Web.App/wwwroot/js/history-timeline.js` — D3.js osa, zoom, filtry
- `Tobiso.Web.App/Components/Shared/EventPopup.razor` — popup s obsahem článku

**Změny stávajících souborů:**
- Navigace: odkaz "Dějepis" v menu → `/history` místo `/categories/{id}`
- `Tobiso.Web.App.Admin` — rozšíření editoru událostí o GeoArea, LinkedPostId, Importance

**Technologie:**
- D3.js (již existuje pro knowledge graph) — rozšíření pro timeline
- Zoom: `d3.zoom()` na časové ose
- Drag: horizontální drag pro posouvání

**Odhadovaná práce:** 6–8 dní (základní timeline) + 5–7 dní (živá mapa, viz níže)

---

### Fáze 28b – Živá historická mapa (rozšíření)

**Co to je:** Vedle timeline se zobrazí mapa světa/Evropy, která se mění v čase. Jak posouváš osu, mapa ukazuje, která území existovala ve stejnou dobu — překrývající se říše, měnící se hranice, vznikající a zanikající státy.

```
/history

──────[ 1350 ]──────────────────────────────────────────►
         ▲ scrubber

┌─────────────────────────────────────────────────────┐
│                    MAPA roku 1350                   │
│                                                     │
│   ░░░░ Říše Karla IV.   ████ Francie                │
│   ████ Polsko           ░░░░ Uhersko                │
│   ████ Osmanská říše (vzniká...)                    │
│                                                     │
│   Hover na území → "Česká koruna, 1310–1419         │
│                     → [Článek: Karel IV.]"          │
└─────────────────────────────────────────────────────┘
```

**Klíčové vlastnosti:**
- **Scrubber** — tažením po časové ose se mapa plynule mění
- **Překryvy** — více říší/států může existovat ve stejném čase (vykresleny vrstvami s průhledností)
- **Vznik a zánik** — území se "rozsvítí" když stát vznikne, "zhasne" když zanikne
- **Hover na území** → tooltip se jménem, léty existence, a odkazem na článek
- **Klik na území** → popup s článkem (stejný jako klik na bod v timeline)
- **Filtr oblasti** synchronizovaný s timeline filtry (Čechy, Evropa, Svět...)

**Data:**
- GeoJSON polygony pro historická území (OpenHistoricalMap nebo ruční tvorba)
- Každý polygon: `{ name, startYear, endYear, geoJson, linkedPostId, color }`
- Nová tabulka: `HistoricalTerritories` s GeoJSON uloženým jako TEXT

```sql
CREATE TABLE HistoricalTerritories (
    Id INT PRIMARY KEY,
    Name NVARCHAR(200),
    StartYear INT,
    EndYear INT,
    GeoJson TEXT,           -- GeoJSON polygon
    Color NVARCHAR(10),     -- hex barva
    LinkedPostId INT,       -- odkaz na článek
    Area NVARCHAR(100)      -- 'cechy', 'evropa', ...
);
```

**Technologie:**
- Leaflet.js (přidáváme pro Fázi 3) — historické polygony jako GeoJSON vrstvy
- Animace přechodů: `L.geoJSON` vrstvy se přidávají/odebírají při scrubování
- Data z OpenHistoricalMap (open-source historické mapy) nebo manuální GeoJSON

**Odhadovaná práce:** 10–15 dní

> ⚠️ **Největší riziko celého plánu — data.** Původní odhad 5–7 dní předpokládal dostupná GeoJSON data. Ve skutečnosti:
> - OpenHistoricalMap má neúplné pokrytí pro střední Evropu před 1500
> - Ruční tvorba polygonů pro Přemyslovce, Lucemburky, Habsburky = týdny práce
> - Doporučeno: nejdřív ověřit dostupnost dat *před* zahájením implementace. Pokud data neexistují, celá Fáze 28b není realizovatelná bez samostatného datového projektu.
> - Alternativa: začít jen s moderní mapou (1800–dnes) kde data existují, historické polygony přidávat postupně.
> - **GeoJSON tvorba:** geojson.io funguje pro ruční kreslení polygonů a bodů – výstup přímo vložitelný do `HistoricalTerritories.GeoJson`.

---

## Fáze 0 – Uživatelské účty (prerekvizita pro fáze 26, 27 a AI historii)

**Co to je:** Systém studentských účtů (registrace, přihlášení, profil). Oddělený od stávajícího admin Basic Auth — studenti mají vlastní JWT tokeny s `role: "student"` claimem. Obsahuje historii AI chatů, kreditový systém a základ pro vše co vyžaduje identitu.

**Odhadovaná práce:** 6–8 dní

---

### Krok 1 – Databázové entity

**Nové soubory v `Tobiso.Web.Domain/Entities/`:**

```csharp
// AppUser.cs
public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";   // PBKDF2
    public int Credits { get; set; } = 20;            // startovní kredit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<AiChatSession> ChatSessions { get; set; } = [];
    public ICollection<AiCreditTransaction> CreditTransactions { get; set; } = [];
    public ICollection<UserBookmark> Bookmarks { get; set; } = [];
    public ICollection<UserReadPost> ReadPosts { get; set; } = [];
}

// AiChatSession.cs  (jedna konverzace = jedno vlákno zpráv u jednoho článku)
public class AiChatSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
    public ICollection<AiChatMessage> Messages { get; set; } = [];
}

// AiChatMessage.cs
public class AiChatMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Role { get; set; } = "";   // "user" | "assistant"
    public string Content { get; set; } = "";
    public int? CreditsUsed { get; set; }    // null = user zpráva, číslo = AI odpověď
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiChatSession Session { get; set; } = null!;
}

// AiCreditTransaction.cs
public class AiCreditTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Delta { get; set; }           // kladné = dobití, záporné = utracení
    public string Reason { get; set; } = ""; // "ai_ask" | "registration_bonus" | "admin_grant"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}

// UserBookmark.cs
public class UserBookmark
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}

// UserReadPost.cs  (pro learning path + streak)
public class UserReadPost
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PostId { get; set; }
    public int ScrollPercent { get; set; }   // 0–100
    public DateTime FirstReadAt { get; set; } = DateTime.UtcNow;
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
```

**Registrace v `TobisoDbContext`:**

```csharp
public DbSet<AppUser> Users => Set<AppUser>();
public DbSet<AiChatSession> AiChatSessions => Set<AiChatSession>();
public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();
public DbSet<AiCreditTransaction> AiCreditTransactions => Set<AiCreditTransaction>();
public DbSet<UserBookmark> UserBookmarks => Set<UserBookmark>();
public DbSet<UserReadPost> UserReadPosts => Set<UserReadPost>();
```

**Migration:**

```bash
dotnet ef migrations add AddUserAccounts \
  --project Tobiso.Web.Api \
  --startup-project Tobiso.Web.App \
  --output-dir Infrastructure/Data/Migrations
dotnet ef database update \
  --project Tobiso.Web.Api \
  --startup-project Tobiso.Web.App
```

---

### Krok 2 – Password hashing (PBKDF2, žádná závislost navíc)

**Nový soubor `Tobiso.Web.Api/Authentication/PasswordHasher.cs`:**

```csharp
public static class PasswordHasher
{
    // Returns "PBKDF2:iter:salt:hash" — vše v Base64
    public static string Hash(string password)
    {
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2:{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 4 || parts[0] != "PBKDF2") return false;
        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
```

---

### Krok 3 – UserService

**Nový soubor `Tobiso.Web.Api/Services/UserService.cs`:**

```csharp
public interface IUserService
{
    Task<AppUser?> RegisterAsync(string email, string displayName, string password);
    Task<AppUser?> LoginAsync(string email, string password);
    Task<AppUser?> GetByIdAsync(int id);
    Task<bool> DeductCreditsAsync(int userId, int amount, string reason);
    Task AddCreditsAsync(int userId, int amount, string reason);
}

public class UserService : IUserService
{
    private readonly TobisoDbContext _db;

    public UserService(TobisoDbContext db) => _db = db;

    public async Task<AppUser?> RegisterAsync(string email, string displayName, string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email.ToLower()))
            return null; // email již existuje

        var user = new AppUser
        {
            Email = email.ToLower(),
            DisplayName = displayName,
            PasswordHash = PasswordHasher.Hash(password),
            Credits = 20  // registrační bonus
        };
        _db.Users.Add(user);
        _db.AiCreditTransactions.Add(new AiCreditTransaction
        {
            User = user, Delta = 20, Reason = "registration_bonus"
        });
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
            return null;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return user;
    }

    public Task<AppUser?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> DeductCreditsAsync(int userId, int amount, string reason)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Credits < amount) return false;
        user.Credits -= amount;
        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = -amount, Reason = reason });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AddCreditsAsync(int userId, int amount, string reason)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return;
        user.Credits += amount;
        _db.AiCreditTransactions.Add(new AiCreditTransaction
            { UserId = userId, Delta = amount, Reason = reason });
        await _db.SaveChangesAsync();
    }
}
```

Registrace v `Program.cs`:

```csharp
builder.Services.AddScoped<IUserService, UserService>();
```

---

### Krok 4 – JWT rozšíření (student tokeny)

Stávající `ManualJwtAuthHandler` zůstane beze změn. `JwtTokenService` se rozšíří o metodu pro studenty:

**Upravit `Tobiso.Web.App/Authentication/JwtTokenService.cs`:**

```csharp
// Přidat vedle stávající GenerateToken() pro adminy:
public string GenerateStudentToken(AppUser user)
{
    var claims = new Dictionary<string, object>
    {
        ["sub"] = user.Id.ToString(),
        ["name"] = user.DisplayName,
        ["email"] = user.Email,
        ["role"] = "student",
        ["exp"] = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
    };
    return BuildToken(claims); // sdílená privátní metoda
}
```

Token pak nese claim `role = "student"` → lze ho rozlišit od admin tokenů v middleware.

---

### Krok 5 – AuthController rozšíření

**Přidat do `Tobiso.Web.App/Controllers/AuthController.cs`:**

```csharp
[HttpPost("register")]
[AllowAnonymous]
public async Task<IActionResult> Register([FromBody] RegisterRequest req)
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return BadRequest(new { message = "Email a heslo jsou povinné." });

    var user = await _userService.RegisterAsync(req.Email, req.DisplayName ?? req.Email, req.Password);
    if (user == null)
        return Conflict(new { message = "Email je již zaregistrován." });

    var token = _jwtService.GenerateStudentToken(user);
    return Ok(new { token, displayName = user.DisplayName, credits = user.Credits });
}

[HttpPost("student-login")]
[AllowAnonymous]
public async Task<IActionResult> StudentLogin([FromBody] LoginRequest req)
{
    var user = await _userService.LoginAsync(req.Username, req.Password);
    if (user == null)
        return Unauthorized(new { message = "Nesprávný email nebo heslo." });

    var token = _jwtService.GenerateStudentToken(user);
    return Ok(new { token, displayName = user.DisplayName, credits = user.Credits });
}

[HttpGet("me")]
[Authorize]  // funguje pro oba — admin i student JWT
public async Task<IActionResult> Me()
{
    var role = User.FindFirst("role")?.Value;
    if (role == "student")
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        return Ok(new { displayName = user.DisplayName, email = user.Email,
                        credits = user.Credits, role = "student" });
    }
    return Ok(new { displayName = User.Identity?.Name, role = "admin" });
}
```

**DTOs (`Tobiso.Web.Shared/DTOs/`):**

```csharp
public record RegisterRequest(string Email, string? DisplayName, string Password);
// LoginRequest již existuje
```

---

### Krok 6 – AI chat historie

**Nový soubor `Tobiso.Web.Api/Services/AiChatHistoryService.cs`:**

```csharp
public interface IAiChatHistoryService
{
    Task<AiChatSession> GetOrCreateSessionAsync(int userId, int postId);
    Task SaveMessageAsync(int sessionId, string role, string content, int? creditsUsed = null);
    Task<List<AiChatSession>> GetUserSessionsAsync(int userId);
    Task<List<AiChatMessage>> GetSessionMessagesAsync(int sessionId, int userId);
}
```

**Upravit `Tobiso.Web.App/Controllers/AiController.cs` – metoda `Ask`:**

```csharp
// Na konci Ask(), po úspěšné AI odpovědi:
if (User.Identity?.IsAuthenticated == true
    && User.FindFirst("role")?.Value == "student"
    && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
{
    // Odečíst 1 kredit za AI odpověď
    var hasCredits = await _userService.DeductCreditsAsync(userId, 1, "ai_ask");
    if (!hasCredits)
        return StatusCode(402, new { message = "Nemáš dostatek kreditů." });

    var session = await _chatHistoryService.GetOrCreateSessionAsync(userId, request.PostId);
    await _chatHistoryService.SaveMessageAsync(session.Id, "user", request.Question);
    await _chatHistoryService.SaveMessageAsync(session.Id, "assistant", aiResponse, creditsUsed: 1);
}
```

**Nový endpoint pro historii:**

```csharp
[HttpGet("history")]
[Authorize]
public async Task<IActionResult> GetHistory()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var sessions = await _chatHistoryService.GetUserSessionsAsync(userId);
    return Ok(sessions);
}

[HttpGet("history/{sessionId}")]
[Authorize]
public async Task<IActionResult> GetSession(int sessionId)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var messages = await _chatHistoryService.GetSessionMessagesAsync(sessionId, userId);
    return Ok(messages);
}
```

> **Kredit logika pro anonymní uživatele:** Stávající IP/device rate limiting zůstane pro nepřihlášené. Přihlášení studenti místo toho jdou přes kreditový systém.

---

### Krok 7 – Blazor UI

**Nové soubory v `Tobiso.Web.App/Components/`:**

```
Pages/
  Register.razor         → /registrace
  Login.razor            → /prihlaseni
  Profile.razor          → /profil
  ChatHistory.razor      → /profil/chaty

Shared/
  AuthState.razor        → sdílený stav přihlášení (Cascading)
  UserMenu.razor         → avatar + menu v navbaru (přihlášen / nepřihlášen)
  CreditBadge.razor      → zobrazení "💎 14 kreditů" v navbaru
```

**`AuthState` – správa JWT v Blazor Server:**

```csharp
// Uložení tokenu do localStorage při přihlášení:
await JSRuntime.InvokeVoidAsync("localStorage.setItem", "tobiso_token", token);

// Čtení při startu:
var token = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "tobiso_token");
```

Protože Blazor Server nemůže přistoupit k localStorage při SSR prerenderu, token se načte přes JS interop po `OnAfterRenderAsync`.

**`Login.razor` – formulář:**

```razor
@page "/prihlaseni"

<div class="auth-card">
    <h1>Přihlásit se</h1>
    <input @bind="email" type="email" placeholder="Email" />
    <input @bind="password" type="password" placeholder="Heslo" />
    <button @onclick="DoLogin">Přihlásit</button>
    <p>Nemáš účet? <a href="/registrace">Zaregistruj se</a></p>
</div>
```

**`Profile.razor`** – zobrazí displayName, email, počet kreditů, historii chatů (seznam sessions s názvem článku + datum), záložky (bookmarks), přečtené články.

---

### Krok 8 – Kreditový systém (přehled)

| Akce | Delta |
|------|-------|
| Registrace | +20 |
| Každý den (denní bonus) | +20 |
| AI odpověď | −1 |
| Přečtení celého článku (>80% scroll) | +1 |

**Denní bonus endpoint:**

```csharp
[HttpPost("daily-bonus")]
[Authorize]
public async Task<IActionResult> ClaimDailyBonus()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var user = await _userService.GetByIdAsync(userId);
    if (user?.LastLoginAt?.Date == DateTime.UtcNow.Date)
        return Conflict(new { message = "Bonus již byl dnes vybrán." });
    await _userService.AddCreditsAsync(userId, 20, "daily_bonus");
    return Ok(new { credits = user!.Credits + 20 });
}
```

**Admin správa kreditů** (v Admin appu):

```
GET  /api/admin/users           → seznam uživatelů
POST /api/admin/users/{id}/credits  → { delta, reason }
```

---

### Krok 9 – Admin app rozšíření

**Nová admin stránka `Tobiso.Web.App.Admin/Components/Pages/Users.razor`:**

- Tabulka uživatelů (email, displayName, credits, createdAt, lastLoginAt)
- Tlačítko "Přidat kredity" → modal s `delta` a `reason`
- Tlačítko "Deaktivovat"
- Zobrazení počtu AI chatů per user

---

### Shrnutí závislostí

```
Fáze 0 (Účty) ──► Fáze 10 (Learning Path)
               ──► Fáze 20 (Streak & badges)
               ──► Fáze 26 (Teacher tools)
               ──► Fáze 27 (Spaced repetition)
               ──► AI chat historie
               ──► Kreditový systém
               ──► Záložky v DB (místo localStorage)
```

**Soubory k vytvoření:**

| Soubor | Projekt |
|--------|---------|
| `Domain/Entities/AppUser.cs` | `Tobiso.Web.Domain` |
| `Domain/Entities/AiChatSession.cs` | `Tobiso.Web.Domain` |
| `Domain/Entities/AiChatMessage.cs` | `Tobiso.Web.Domain` |
| `Domain/Entities/AiCreditTransaction.cs` | `Tobiso.Web.Domain` |
| `Domain/Entities/UserBookmark.cs` | `Tobiso.Web.Domain` |
| `Domain/Entities/UserReadPost.cs` | `Tobiso.Web.Domain` |
| `Api/Authentication/PasswordHasher.cs` | `Tobiso.Web.Api` |
| `Api/Services/UserService.cs` | `Tobiso.Web.Api` |
| `Api/Services/AiChatHistoryService.cs` | `Tobiso.Web.Api` |
| `App/Components/Pages/Register.razor` | `Tobiso.Web.App` |
| `App/Components/Pages/Login.razor` | `Tobiso.Web.App` |
| `App/Components/Pages/Profile.razor` | `Tobiso.Web.App` |
| `App/Components/Pages/ChatHistory.razor` | `Tobiso.Web.App` |
| `App/Components/Shared/UserMenu.razor` | `Tobiso.Web.App` |
| `App/Components/Shared/CreditBadge.razor` | `Tobiso.Web.App` |
| `App.Admin/Components/Pages/Users.razor` | `Tobiso.Web.App.Admin` |

**Soubory k úpravě:**

| Soubor | Změna |
|--------|-------|
| `TobisoDbContext.cs` | DbSety pro nové entity |
| `App/Controllers/AuthController.cs` | `/register`, `/student-login`, `/me` |
| `App/Controllers/AiController.cs` | Kreditová kontrola, ukládání do historie |
| `App/Authentication/JwtTokenService.cs` | `GenerateStudentToken()` |
| `App/Components/Shared/NavMenu.razor` | `UserMenu` + `CreditBadge` |
| `Shared/DTOs/` | `RegisterRequest`, `UserProfileDto` |
