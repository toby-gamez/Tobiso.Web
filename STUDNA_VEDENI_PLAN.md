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
| 13 | Reading progress & bookmarks | ✅ Done |
| 14 | Exam predictor | ✅ Done |
| 15 | Surprise facts sidebar card | ✅ Done |
| 16 | Socratic tutor mode | 🔲 Not started |
| 17 | Difficulty rewrite (register switch) | ✅ Done |
| 18 | Personal notes | ✅ Done |
| 19 | Study timer (Pomodoro) | ✅ Done |
| 20 | Reading streak & subject badges | ✅ Done |
| 21 | Difficulty rating by students | ✅ Done |
| 22 | Random article / Article of the day | ✅ Done |
| 23 | Comparison tables (AI generated) | 🔲 Not started |
| 24 | Step-by-step solver | 🔲 Not started |
| 25 | Video context card | ✅ Done |
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

BLOK 2 – Rychlé výhry (nezávislé, žádné prerekvizity) ✅ HOTOVO
  ├─ Fáze 22  Náhodný článek / Článek dne                                  ✅ Done
  ├─ Fáze 15  Surprise facts sidebar ("Věděl jsi, že…?")                   ✅ Done
  ├─ Fáze 14  Exam predictor ("Co bude v testu?")                          ✅ Done
  ├─ Fáze 21  Difficulty rating (😊/😐/😕 po přečtení)                      ✅ Done
  ├─ Fáze 17  Difficulty rewrite (8letý / gymnázium / odborník)            ✅ Done
  ├─ Fáze 19  Study timer (Pomodoro)                                       ✅ Done
  └─ Fáze 25  Video context card (YouTube embed)                           ✅ Done

BLOK 3 – Osobní funkce (využívají účty z Bloku 1) ✅ HOTOVO
  ├─ Fáze 13  Reading progress & záložky (scroll %, bookmarks v DB)        ✅ Done
  ├─ Fáze 18  Personal notes (per-článek poznámky)                         ✅ Done
  └─ Fáze 20  Reading streak & subject badges                              ✅ Done

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

**Graf mód:** 3D galaxy z Three.js — 350 článků jako hvězdy v prostoru
- Každý předmět = barevná mlhovina (nebula cluster) v 3D prostoru
- Drag myší = rotace / zoom = přiblížení do clusteru
- Klik na hvězdu = article preview panel (bez opuštění scény)
- Dvojklik = přechod na plný článek
- Volitelný fallback na D3.js 2D pro zařízení bez WebGL

**Mapa mód:** Zeměpisná mapa s piny článků (Leaflet.js)
- Přepnutí z 3D galaxy na mapu jedním kliknutím
- Clustery článků ze stejné oblasti

**Osa mód:** Horizontální timeline všech historických událostí
- Zoom na období
- Klik = article preview

**Nové soubory:**
- `Tobiso.Web.App/Components/Pages/Explore.razor`
- `Tobiso.Web.App/wwwroot/js/explore-galaxy.js` (Three.js 3D galaxy)
- `Tobiso.Web.App/wwwroot/js/explore-graph.js` (D3.js 2D fallback)

**Nové API endpointy:**
- `GET /api/explore/graph` – optimalizovaná data pro celý graf (jen id, title, categoryId, tags)
- `GET /api/explore/geo` – všechny geolokace článků

**Technologie:** Three.js (primární), D3.js fallback, WebGL detekce

**Odhadovaná práce:** 7–9 dní (o 2 dny víc než D3.js varianta kvůli Three.js scéně a kamera ovládání)

---

## Technologický stack (nový)

| Knihovna | Účel | Stav |
|----------|------|------|
| D3.js v7 | Graf znalostí (2D) | ✅ Existuje |
| Leaflet.js | Mapy | 🔲 Přidat |
| OpenStreetMap | Map tiles (zdarma) | 🔲 Přidat |
| Three.js | 3D galaxy explore, 3D fyzikální simulace | 🔲 Přidat |

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
- **Zákon akce a reakce**: dva objekty s animovanými šipkami sil, posuvník hmotnosti (Canvas 2D)
- **Ohmův zákon**: interaktivní obvod, sliders R/U, živý výpočet I (Canvas 2D)
- **Historická mapa**: SVG mapa s animovaným šířením říší (Karel IV. → rozšíření území)
- **Fotosyntéza**: animace vstupu/výstupu molekul v buňce (Canvas 2D)
- **Pythagorova věta**: drag&drop trojúhelník, vizuální proof (Canvas 2D)
- **3D atom / molekula**: Three.js scéna s rotujícím atomem, elektrony na oběžných drahách
- **3D kolize těles**: Two spheres v Three.js, slider hmotnosti/rychlosti, reálná fyzika

> **Poznámka k Three.js widgetům:** GPT-4o může generovat vanilla Three.js kód (CDN import) přímo do `<iframe srcdoc>`. Je to složitější než Canvas, ale vizuálně výrazně silnější pro fyziku a chemii. Prompt šablona má dvě verze — 2D (Canvas/SVG) a 3D (Three.js) — AI vybere podle tématu.

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

**3D rozšíření (Three.js):** Vedle sliderů volitelná 3D vizualizace výsledku:
- **F=ma** → 3D koule pohybující se po ploše s animovaným vektorem síly
- **PV=nRT** → 3D kontejner s pohybujícími se částicemi, hustota odpovídá tlaku
- **E=mc²** → abstraktní particle efekt — energie se "materializuje" do hmoty

Přepínač 2D / 3D ve Formula Playground UI. Three.js scéna se inicializuje na požádání (ne výchozí — výkon).

**Předměty:** fyzika (F=ma, U=RI, E=mc²), chemie (PV=nRT), matematika

**Odhadovaná práce:** 3–4 dny (2D), +2 dny pro 3D vizualizace

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

---

## Interaktivní funkce per článek

> Tento seznam mapuje každý článek na konkrétní interaktivní funkce z fází 3–25. Funkce jako ExamPredictor, SurpriseFacts, DifficultyRewrite, SocraticMode a DefinitionTooltips platí pro VŠECHNY články a nejsou zde zopakovány. Zde jsou jen funkce specifické pro obsah daného článku.

---

## FYZIKA

### Tření
- **FormulaPlayground**: F = μ × N; slider pro hmotnost a součinitel tření
- **AiInteractiveDemo**: Simulace statického vs. kinetického tření, pohyb po nakloněné rovině
- **CrossSubject**: → Hmotnost, Tlak, Obsah; tření v pneumatikách → Newtonovy zákony
- **ComparisonTable**: Statické tření vs. kinetické tření; kuličkové ložisko vs. válečkové ložisko
- **ConceptMap**: třecí síla, statické tření, kinetické tření, ložisko, koeficient tření

### Teplo
- **FormulaPlayground**: Q = m × c × ΔT; slidery pro hmotnost, měrné teplo, teplotní rozdíl
- **AiInteractiveDemo**: Vizualizace vedení/proudění/záření tepla, animace ohřevu/ochlazení
- **CrossSubject**: Teplo → Energie; vedení tepla → Elektrické vodiče; tepelná roztažnost → Skupenství (chemie)
- **ComparisonTable**: Teplo vs. teplota; vedení vs. proudění vs. záření tepla
- **ConceptMap**: teplo, teplota, jouly, vedení, proudění, záření

### Teplota
- **FormulaPlayground**: Převody Celsius ↔ Kelvin ↔ Fahrenheit; interaktivní teploměr
- **AiInteractiveDemo**: Animace molekulárního pohybu při různých teplotách
- **CrossSubject**: Teplota → Teplo; teplota → Změna skupenství; teplota → Podnebí Česka
- **ComparisonTable**: Celsius vs. Kelvin vs. Fahrenheit
- **ConceptMap**: teplota, Celsius, Kelvin, Fahrenheit, tepelná rovnováha

### Rychlost
- **FormulaPlayground**: v = s / t; slidery pro dráhu a čas, auto-výpočet; převody km/h ↔ m/s
- **StepBySolver**: Auto 120 km za 3 hod → v = ?; pohybové úlohy
- **CrossSubject**: Rychlost → Dráha → Čas; rychlost → Newtonovy zákony; rychlost zvuku → Akustika
- **ConceptMap**: rychlost, dráha, čas, tachometr, průměrná rychlost

### Dráha
- **FormulaPlayground**: s = v × t; slider pro rychlost a čas
- **StepBySolver**: Základní pohybové příklady s výpočtem dráhy
- **CrossSubject**: Dráha → Rychlost → Čas; dráha → Práce (W = F × s)
- **ConceptMap**: dráha, metr, vzdálenost, pohyb

### Čas
- **FormulaPlayground**: Převody s/min/h/den
- **AiInteractiveDemo**: Interaktivní časová osa nebo stopky s vizualizací
- **CrossSubject**: Čas → Rychlost → Dráha; čas → Výkon (P = W/t)
- **ConceptMap**: čas, sekunda, hodiny, stopky

### Energie
- **FormulaPlayground**: Eₖ = ½mv², Eₚ = mgh; slidery pro hmotnost, výšku, rychlost
- **AiInteractiveDemo**: Animace přeměny polohové energie na pohybovou (padající těleso, kyvadlo)
- **CrossSubject**: Energie → Práce → Výkon; kinetická energie → Rychlost + Hmotnost; energie → Elektrárny
- **ComparisonTable**: Pohybová energie vs. polohová energie vs. vnitřní energie
- **ConceptMap**: energie, kinetická, polohová, joule, zákon zachování energie

### Tlak
- **FormulaPlayground**: p = F / S; slidery pro sílu a plochu, výpočet v kPa
- **StepBySolver**: Krabice 5 kg, plocha 1 dm² → tlak na podlahu
- **AiInteractiveDemo**: Animace tlaku pod vodou, pneumatiky, hydraulika
- **CrossSubject**: Tlak → Síla → Obsah; hydrostatický tlak → Kapaliny; osmóza → Biologie → Chemie
- **ConceptMap**: tlak, pascal, síla, plocha

### Objem
- **FormulaPlayground**: Vzorce objemu pro různá tělesa; slider pro rozměry
- **CrossSubject**: Objem → Hustota → Hmotnost; objem → Kapaliny; molární objem (chemie)
- **ConceptMap**: objem, m³, odměrný válec, hustota

### Hmotnost
- **FormulaPlayground**: F = m × g (10 N/kg); slovní příklady
- **CrossSubject**: Hmotnost → Hustota (ρ = m/V); hmotnost → Tlak; hmotnost → Energie
- **ConceptMap**: hmotnost, kilogram, váha, gravitace

### Hustota
- **FormulaPlayground**: ρ = m / V; slider pro hmotnost a objem
- **StepBySolver**: Zlatý předmět 193 g, 10 cm³ — je to zlato?
- **CrossSubject**: Hustota → Kapaliny (plovoucí tělesa); hustota → Archimedův zákon; hustota → Složení látek (chemie)
- **ComparisonTable**: Hustota vody vs. oleje vs. zlata vs. vzduchu
- **ConceptMap**: hustota, kg/m³, hustoměr, objem, hmotnost

### Obsah
- **FormulaPlayground**: Vzorce obsahu plochy (čtverec, obdélník, trojúhelník, kruh)
- **CrossSubject**: Obsah → Tlak (p = F/S); obsah → Geometrie
- **ConceptMap**: obsah, plocha, m², vzorec

### Délka
- **FormulaPlayground**: Převody mm/cm/dm/m/km
- **CrossSubject**: Délka → Dráha; délka → Geometrické výpočty
- **ConceptMap**: délka, metr, metrická soustava

### Elektrický proud
- **FormulaPlayground**: I = U / R (Ohmův zákon); slider pro napětí a odpor
- **CrossSubject**: Proud → Napětí → Odpor; proud → Elektřina; proud → Elektrolýza (chemie)
- **ConceptMap**: elektrický proud, ampér, ampérmetr, Ohmův zákon

### Výkon
- **FormulaPlayground**: P = W / t; slider pro práci a čas; P = U × I pro elektro
- **StepBySolver**: Petra leze do výšky 1,5 m za 7,5 s, váha 50 kg → výkon?
- **CrossSubject**: Výkon → Práce → Čas; elektrický výkon → Elektrárny
- **ConceptMap**: výkon, watt, práce, čas, joule

### Práce
- **FormulaPlayground**: W = F × s; slider pro sílu a dráhu
- **StepBySolver**: Nesu 8 kg batoh 20 km → jakou práci vykonám?
- **CrossSubject**: Práce → Výkon → Čas; práce → Energie; práce → Jednoduché stroje
- **ConceptMap**: práce, joule, síla, dráha, mechanická práce

### Síla
- **FormulaPlayground**: F = m × a nebo F = m × g; různé typy sil
- **AiInteractiveDemo**: Diagram silového rozboru, vektorový součet sil
- **CrossSubject**: Síla → Tlak; Síla → Práce; Síla → Newtonovy zákony; Gravitace → Vesmír
- **ComparisonTable**: Gravitační vs. tlaková vs. třecí vs. elektrická síla
- **ConceptMap**: síla, newton, siloměr, gravitace, výslednice

### Světlo a optika
- **FormulaPlayground**: Zákon odrazu; výpočet indexu lomu
- **AiInteractiveDemo**: Simulace odrazu a lomu paprsku; rozklad bílého světla hranolem
- **CrossSubject**: Optika → Oko (smyslová soustava); čočky → Geometrie; světlo → Vesmír — Slunce
- **ComparisonTable**: Sbiehavá čočka vs. rozbiehavá; dalekozrakost vs. krátkozrakost
- **ConceptMap**: světlo, odraz, lom, čočka, index lomu, spektrum

### Akustika
- **FormulaPlayground**: v = λ × f; slider pro frekvenci a vlnovou délku zvuku
- **AiInteractiveDemo**: Vizualizace zvukových vln, amplituda/frekvence; Dopplerův efekt
- **CrossSubject**: Akustika → Hudební teorie (frekvence not); zvuk → Smyslová soustava (ucho)
- **ComparisonTable**: Ultrazvuk vs. infrazvuk vs. slyšitelné pásmo
- **ConceptMap**: zvuk, frekvence, amplituda, vlnění, decibel, ultrazvuk

### Elektřina
- **FormulaPlayground**: U = R × I; Kirchhoffovy zákony; výpočet ceny elektřiny
- **AiInteractiveDemo**: Interaktivní schéma elektrického obvodu — přidávání rezistorů, žárovek
- **CrossSubject**: Elektřina → Elektrárny; elektřina → Elektrolýza (chemie); elektřina → Ionty (chemie)
- **ComparisonTable**: Sériové zapojení vs. paralelní zapojení
- **ConceptMap**: elektrický obvod, proud, napětí, odpor, rezistor, vodič

### Elektrický odpor
- **FormulaPlayground**: R = U / I; výpočet odporu sériového a paralelního zapojení
- **StepBySolver**: Napětí 12 V, proud 2 A → odpor?
- **CrossSubject**: Odpor → Proud → Napětí; odpor → Materiálové vlastnosti (chemie)
- **ConceptMap**: odpor, ohm, Ohmův zákon, reostat, vodič, izolant

### Elektrické napětí
- **FormulaPlayground**: U = R × I; přepočet V/kV/mV
- **CrossSubject**: Napětí → Odpor → Proud; napětí → Galvanický článek (chemie)
- **ConceptMap**: napětí, volt, voltmetr, zdroj napětí

### Elektromagnetismus
- **AiInteractiveDemo**: Animace magnetického pole kolem vodiče s proudem; dynamo a elektromotor; buzola
- **CrossSubject**: Elektromagnetismus → Elektrárny; magnetismus → Vesmír (sluneční vítr)
- **ComparisonTable**: Dynamo vs. motor; střídavý proud vs. stejnosměrný proud
- **ConceptMap**: elektromagnetismus, magnet, dynamo, elektromotor, Nikola Tesla

### Elektrárny
- **GeoMap**: Elektrárny v ČR — Dukovany, Temelín, vodní (Lipno, Orlík), větrné parky
- **AiInteractiveDemo**: Animace přenosu elektřiny od elektrárny ke spotřebiteli; přečerpávací elektrárna
- **CrossSubject**: Elektrárny → Elektřina; větrné elektrárny → Globální změny klimatu
- **ComparisonTable**: Jaderná vs. vodní vs. větrná vs. solární elektrárna — výhody/nevýhody
- **ConceptMap**: elektrárna, transformátor, přenosová soustava, obnovitelné zdroje, jaderná energie

### Změna skupenství
- **FormulaPlayground**: Q = m × L (latentní teplo); slider pro hmotnost a skupenské teplo
- **AiInteractiveDemo**: Animace přechodů mezi skupenstvími (tání, var, sublimace…)
- **CrossSubject**: Změna skupenství → Voda (chemie); skupenství → Teplo; bod varu → Tlak
- **ComparisonTable**: Tání vs. tuhnutí; vypařování vs. kapalnění; sublimace vs. desublimace
- **ConceptMap**: skupenství, tání, tuhnutí, var, sublimace, latentní teplo

### Radioaktivita
- **HistoryTimeline**: 1898 Curie (radium) → 1938 Hahn (štěpení jádra) → 1945 Hirošima/Nagasaki → 1986 Černobyl → 2011 Fukušima
- **GeoMap**: Hirošima, Nagasaki, Černobyl, Fukušima na mapě světa
- **AiInteractiveDemo**: Animace rozpadu alfa/beta/gama; simulace poločasu rozpadu
- **CrossSubject**: Radioaktivita → Prvky (uran, radon); záření → Elektrárny (jaderná)
- **ComparisonTable**: Alfa záření vs. beta záření vs. gama záření — pronikavost a ochrana
- **ConceptMap**: radioaktivita, alfa/beta/gama záření, poločas rozpadu, radon

### Těžiště
- **AiInteractiveDemo**: Interaktivní hledání těžiště nepravidelného tělesa; balancující těleso
- **CrossSubject**: Těžiště → Páka (jednoduché stroje); těžiště → Trojúhelníky (těžnice v geometrii)
- **ConceptMap**: těžiště, rovnováha, hmotnost, těleso, střed hmotnosti

### Kapaliny
- **FormulaPlayground**: Hydrostatický tlak p = ρ × g × h; Archimedův zákon F = ρ × V × g
- **AiInteractiveDemo**: Animace spojených nádob, plovoucí těleso vs. klesající
- **CrossSubject**: Kapaliny → Hustota; kapaliny → Tlak; kapaliny → Voda (chemie); osmotický tlak → Biologie
- **ComparisonTable**: Kapalina vs. plyn vs. pevná látka — stlačitelnost a tvar
- **ConceptMap**: kapalina, hydrostatický tlak, viskozita, povrchové napětí, Archimedův zákon

### Newtonovy pohybové zákony
- **FormulaPlayground**: F = m × a (2. zákon); slider pro hmotnost a zrychlení
- **AiInteractiveDemo**: Simulace 1./2./3. zákona — setrvačnost, akce-reakce
- **CrossSubject**: Newtonovy zákony → Tření; zákony → Jednoduché stroje; 3. zákon → Raketa (vesmír)
- **ComparisonTable**: 1. zákon (setrvačnost) vs. 2. zákon (síla) vs. 3. zákon (akce-reakce)
- **ConceptMap**: Newtonovy zákony, setrvačnost, zrychlení, akce, reakce

### Motory
- **AiInteractiveDemo**: Animace čtyřdobého spalovacího motoru (sání–komprese–expanze–výfuk)
- **CrossSubject**: Motory → Energie (přeměna chemické na mechanickou); motory → Sekundér (průmysl)
- **ComparisonTable**: Zážehový motor (benzín) vs. vznětový (nafta); elektromotor vs. spalovací
- **ConceptMap**: motor, spalovací, elektrický, zážehový, vznětový

### Jednoduché stroje
- **FormulaPlayground**: Páka: F₁ × r₁ = F₂ × r₂; slider pro délku ramen a sílu
- **AiInteractiveDemo**: Interaktivní páka, kladka, kladkostroj — změna síly
- **CrossSubject**: Jednoduché stroje → Práce → Síla; páka → Těžiště
- **ComparisonTable**: Páka vs. kladka vs. kladkostroj — výhody, využití
- **ConceptMap**: páka, kladka, kladkostroj, rameno, mechanical advantage

---

## MATEMATIKA

### Křížové pravidlo
- **StepBySolver**: Krok za krokem krácení zkřížením 2/8 × 4/2 → 1/2
- **AiInteractiveDemo**: Vizuální diagram křížového pravidla se zvýrazněnými čitateli/jmenovateli
- **CrossSubject**: Křížové pravidlo → Krácení zlomku → NSD; zlomky → Procenta → Poměr
- **ConceptMap**: zlomek, čitatel, jmenovatel, krácení, křížové pravidlo

### Mocniny a odmocniny
- **FormulaPlayground**: a², a³, √a; slider pro základ a mocnitel; pravidla součinu (aᵐ × aⁿ = aᵐ⁺ⁿ)
- **StepBySolver**: Výpočty mocnin, součin mocnin se stejným základem
- **CrossSubject**: Mocniny → Druhá mocnina (obsah čtverce); odmocniny → Pythagorova věta; Avogadrovo číslo (chemie)
- **ComparisonTable**: Druhá mocnina vs. třetí mocnina; druhá odmocnina vs. třetí odmocnina
- **ConceptMap**: mocnina, odmocnina, základ, mocnitel, čtvercové číslo

### Nepřímá úměrnost
- **FormulaPlayground**: y = k/x; interaktivní graf hyperboly se sliderem pro konstantu k
- **AiInteractiveDemo**: Animace — rychlost × čas = konstantní dráha; kolik dělníků × kolik dní
- **CrossSubject**: Nepřímá úměrnost → Přímá úměrnost (kontrast); → Trojčlenka; → Rychlost × Čas
- **ComparisonTable**: Přímá úměrnost (y = kx) vs. nepřímá (y = k/x)
- **ConceptMap**: nepřímá úměrnost, konstanta, hyperbola, proměnná

### Přímá úměrnost
- **FormulaPlayground**: y = k × x; slider pro konstantu k; vizualizace přímky v grafu
- **AiInteractiveDemo**: Interaktivní graf přímé úměrnosti; reálné příklady (cena × počet kusů)
- **CrossSubject**: Přímá úměrnost → Trojčlenka → Procenta; přímá úměrnost → Lineární funkce
- **ComparisonTable**: Přímá vs. nepřímá úměrnost — chování grafu
- **ConceptMap**: přímá úměrnost, koeficient, lineární funkce, graf, přímka

### Měřítko mapy
- **GeoMap**: Interaktivní mapa ČR — student klikne na dvě místa a spočítá vzdálenost
- **FormulaPlayground**: Skutečná vzdálenost = vzdálenost na mapě × měřítko; slider
- **StepBySolver**: 1:50 000 — 3 cm na mapě = kolik km v terénu?
- **CrossSubject**: Měřítko → Poměr → Trojčlenka; měřítko → Geomorfologie ČR
- **ConceptMap**: měřítko, poměr, mapa, vzdálenost

### Poměr
- **FormulaPlayground**: Rozdělení čísla v poměru a:b; slider pro celkovou hodnotu
- **StepBySolver**: Rozdělit 150 v poměru 2:3
- **CrossSubject**: Poměr → Zlomky → Procenta → Trojčlenka; poměr → Měřítko mapy
- **ConceptMap**: poměr, zlomek, základní tvar, převrácený poměr

### Procenta
- **FormulaPlayground**: 3 vzorce — procentová část, základ, počet procent; slider + textové zadání
- **StepBySolver**: DPH 21 %, půjčka s úrokem, sleva v obchodě
- **CrossSubject**: Procenta → Poměr → Trojčlenka; DPH → Hospodářství; promile → Hustota
- **ComparisonTable**: Procentová část vs. základ vs. počet procent — co hledáme
- **ConceptMap**: procento, základ, část, úrok, DPH, výpočet

### Trojčlenka
- **FormulaPlayground**: Přímá a nepřímá trojčlenka; slider pro 3 known values → výpočet 4. hodnoty
- **StepBySolver**: 3 kg jablek = 60 Kč → 5 kg = ? Kč; pohybové příklady
- **CrossSubject**: Trojčlenka → Přímá/nepřímá úměrnost → Procenta
- **ConceptMap**: trojčlenka, přímá úměrnost, nepřímá úměrnost, proměnná

### Druhá odmocnina
- **FormulaPlayground**: √a; čtvercová čísla; slider pro hodnotu pod odmocninou
- **StepBySolver**: √144, √225, odmocniny čtvercových čísel zpaměti
- **CrossSubject**: Druhá odmocnina → Pythagorova věta; odmocnina → Vzorce pro stranu čtverce
- **ConceptMap**: odmocnina, čtvercové číslo, √, inverzní operace, mocnina

### Lineární rovnice
- **FormulaPlayground**: ax + b = 0; slider pro koeficienty; vizualizace grafu
- **StepBySolver**: Rovnice se zlomkem, pohybové úlohy, úlohy o směsích
- **CrossSubject**: Lineární rovnice → Soustava rovnic; lineární rovnice → Funkce (průsečík s osou)
- **ConceptMap**: rovnice, proměnná, kořen, lineární, koeficient

### Soustavy rovnic
- **StepBySolver**: Dosazovací metoda (2 příklady), sčítací metoda (2 příklady), slovní úlohy
- **AiInteractiveDemo**: Vizualizace soustavy jako dvě přímky v grafu — průsečík = řešení
- **CrossSubject**: Soustavy rovnic → Lineární rovnice; soustavy → Fyzikální slovní úlohy
- **ConceptMap**: soustava rovnic, dosazovací metoda, sčítací metoda, průsečík

### Lomené výrazy
- **FormulaPlayground**: Krácení, rozšiřování, sčítání lomených výrazů; definiční obor
- **StepBySolver**: Krácení (n+1)/(n-1); sčítání se různým jmenovatelem
- **CrossSubject**: Lomené výrazy → Zlomky; lomené výrazy → Lineární rovnice
- **ConceptMap**: lomený výraz, jmenovatel, definiční obor, krácení

### Největší společný dělitel
- **StepBySolver**: Algoritmus dělení prvočísly — D(28,35) = 7 krok za krokem
- **CrossSubject**: NSD → Krácení zlomků → NSN; NSD → Rozklad na prvočísla
- **ComparisonTable**: NSD vs. NSN — co hledáme, jak počítáme
- **ConceptMap**: NSD, dělitelé, prvočísla, rozklad

### Dělitelnost
- **StepBySolver**: Pravidla dělitelnosti 2, 3, 4, 5, 6, 9, 10, 11 s konkrétními příklady
- **AiInteractiveDemo**: Zadej číslo → interaktivní test dělitelnosti všemi pravidly
- **CrossSubject**: Dělitelnost → Prvočísla → Rozklad na prvočísla → NSD/NSN
- **ConceptMap**: dělitelnost, prvočíslo, složené číslo, dělitel, násobek

### Absolutní hodnota
- **FormulaPlayground**: |x|; vizualizace na číselné ose; slider
- **AiInteractiveDemo**: Číselná osa — pohyblivý bod a vzdálenost od nuly
- **CrossSubject**: Absolutní hodnota → Číslo opačné; absolutní hodnota → Znaménka
- **ConceptMap**: absolutní hodnota, vzdálenost od nuly, číselná osa

### Pravoúhlá soustava souřadnic
- **FormulaPlayground**: Zadej souřadnice bodu; vzdálenost dvou bodů
- **AiInteractiveDemo**: Interaktivní graf — kliknutím přidej body; zobraz přímku/parabolu/hyperbolu
- **CrossSubject**: Souřadnice → Funkce (graf); souřadnice → Osová/středová souměrnost
- **ConceptMap**: osa x, osa y, souřadnice, počátek, kvadrant

### Osová souměrnost
- **AiInteractiveDemo**: Interaktivní kreslení — nakresli tvar a zobraz zrcadlový obraz přes osu
- **CrossSubject**: Osová souměrnost → Středová souměrnost; souměrnost → Příroda (motýl, list)
- **ComparisonTable**: Osová souměrnost vs. středová souměrnost
- **ConceptMap**: osa souměrnosti, zrcadlový obraz, symetrie

### Středová souměrnost
- **AiInteractiveDemo**: Interaktivní zobrazení středové souměrnosti
- **ComparisonTable**: Osová souměrnost vs. středová souměrnost — vlastnosti
- **ConceptMap**: střed souměrnosti, obraz, bod, otočení o 180°

### Kruhy a kružnice
- **FormulaPlayground**: Obvod = 2πr; obsah = πr²; slider pro poloměr
- **AiInteractiveDemo**: Vizualizace sečny/tečny/tětivy; vzájemná poloha dvou kružnic
- **CrossSubject**: Kružnice → Trojúhelníky (kružnice opsaná/vepsaná); kružnice → π
- **ConceptMap**: kružnice, kruh, poloměr, průměr, pi, tečna, sečna

### Trojúhelníky
- **FormulaPlayground**: Obvod a + b + c; obsah (a × vₐ)/2; Pythagorova věta; slider pro strany
- **AiInteractiveDemo**: Interaktivní trojúhelník — přetahuj vrcholy; kružnice opsaná/vepsaná
- **CrossSubject**: Trojúhelníky → Trigonometrie; trojúhelníky → Těžiště (fyzika)
- **ComparisonTable**: Rovnostranný vs. rovnoramenný vs. obecný; ostroúhlý vs. pravoúhlý vs. tupoúhlý
- **ConceptMap**: trojúhelník, strany, úhly, výška, těžiště, věty (SSS, SUS, USU)

### Čtyřúhelníky
- **FormulaPlayground**: Obvody a obsahy čtverce, obdélníku, kosočtverce, trapézu; slidery
- **AiInteractiveDemo**: Vizualizace vlastností úhlopříček pro různé čtyřúhelníky
- **ComparisonTable**: Čtverec vs. obdélník vs. kosočtverec vs. lichoběžník
- **ConceptMap**: čtyřúhelník, rovnoběžník, kosodélník, trapéz, úhlopříčka

### 3D tvary
- **FormulaPlayground**: Objem a povrch krychle, kvádru, válce, jehlanu, kužele, koule; slidery
- **AiInteractiveDemo**: Interaktivní 3D rotace těles; rozvinutí do sítě
- **CrossSubject**: 3D tvary → Třetí odmocnina (V = a³ → a = ∛V); objem → Hustota (fyzika)
- **ComparisonTable**: Jehlan vs. kužel; kvádr vs. hranol
- **ConceptMap**: hranol, válec, jehlan, kužel, koule, povrch, objem

### Trigonometrie a goniometrické funkce
- **FormulaPlayground**: sin, cos, tan; slider pro úhel → zobrazení hodnot; Pythagorova věta
- **AiInteractiveDemo**: Interaktivní jednotková kružnice; animace sin/cos jako průmět bodu
- **CrossSubject**: Trigonometrie → Trojúhelníky; sin/cos → Akustika (vlny); trigonometrie → Fyzika (nakloněná rovina)
- **ConceptMap**: sinus, kosinus, tangens, pravoúhlý trojúhelník, goniometrie

### Podobnost
- **FormulaPlayground**: Koeficient podobnosti k = a'/a; výpočet neznámé strany
- **AiInteractiveDemo**: Interaktivní podobné trojúhelníky — měnitelný koeficient
- **CrossSubject**: Podobnost → Měřítko mapy; podobnost → Trigonometrie
- **ConceptMap**: podobnost, koeficient, obraz, odpovídající strany

### Úhly
- **FormulaPlayground**: Sčítání a odčítání úhlů; konverze stupně ↔ radiány
- **AiInteractiveDemo**: Interaktivní goniometr — přetahuj ramena; vedlejší/vrcholové/střídavé úhly
- **CrossSubject**: Úhly → Trojúhelníky (součet = 180°); úhly → Goniometrické funkce; úhly → Optika
- **ComparisonTable**: Ostrý vs. pravý vs. tupý vs. přímý vs. plný úhel
- **ConceptMap**: úhel, stupeň, ramena úhlu, vrcholové úhly

### Konstrukční úlohy (trojúhelníky a rovnoběžníky)
- **AiInteractiveDemo**: Krokový průvodce konstrukcí — animované kreslení kružítkem a pravítkem
- **StepBySolver**: Věty SSS, SUS, USU — konstrukční postupy krok za krokem
- **CrossSubject**: Konstrukce → Množiny bodů; konstrukce → Trojúhelníky
- **ConceptMap**: osa úsečky, osa úhlu, věta SSS, SUS, USU

### Zlomky (sčítání, odčítání, násobení, dělení, krácení, rozšiřování…)
- **FormulaPlayground**: Všechny operace se zlomky; slider pro čitatele a jmenovatele
- **StepBySolver**: Krok za krokem pro každou operaci s konkrétním příkladem
- **CrossSubject**: Zlomky → Procenta → Poměr → Trojčlenka
- **ComparisonTable**: Sčítání zlomků se stejným vs. různým jmenovatelem; krácení vs. rozšiřování
- **ConceptMap**: zlomek, čitatel, jmenovatel, NSD, NSN, společný jmenovatel

---

## CHEMIE

### Rozdělení prvků
- **AiInteractiveDemo**: Interaktivní periodická tabulka — klikni na prvek, zobraz vlastnosti; třídění kovy/nekovy/polokovy
- **CrossSubject**: Prvky → Ionty; prvky → Chemická vazba; prvky → Redoxní reakce
- **ComparisonTable**: Kovy vs. nekovy vs. polokovy; alkalické kovy vs. kovy alkalických zemin
- **ConceptMap**: prvek, kov, nekov, polokov, periodická soustava

### Ionty
- **AiInteractiveDemo**: Animace vzniku iontu — elektron odletí z atomu → vznik kationtu; elektrostatické přitahování
- **CrossSubject**: Ionty → Elektrolýza; ionty → Chemická vazba (iontová); ionty → Oběhová soustava (elektrolyty v krvi)
- **ComparisonTable**: Kation vs. anion — náboj, vznik, příklady
- **ConceptMap**: ion, kation, anion, elektron, elektrolyt

### Vlastnosti látek
- **AiInteractiveDemo**: Virtuální laboratorní experiment — barva, tvrdost, skupenství, rozpustnost
- **CrossSubject**: Vlastnosti → Prvky; vlastnosti → Složení látek; hustota → Fyzika
- **ComparisonTable**: Kvalitativní vs. kvantitativní vlastnosti
- **ConceptMap**: vlastnost, skupenství, hustota, tvrdost, katalyzátor, rozpustnost

### Složení látek
- **AiInteractiveDemo**: Animace stavby atomu — proton, neutron, elektron; vizualizace molekul vody, CO₂
- **CrossSubject**: Složení → Ionty; složení → Chemická vazba; atom → Radioaktivita (fyzika)
- **ComparisonTable**: Atom vs. molekula vs. ion
- **ConceptMap**: atom, molekula, ion, proton, elektron, neutron

### Chemické reakce
- **FormulaPlayground**: Vyčíslení chemické rovnice; slider pro stechiometrické koeficienty
- **AiInteractiveDemo**: Animace reakce — reaktanty → aktivační energie → produkty; exo/endotermická reakce
- **CrossSubject**: Chemické reakce → Mol; reakce → Neutralizace → Redoxní reakce; spalování → CO₂ → Klima
- **ComparisonTable**: Exotermická vs. endotermická; slučování vs. rozklad vs. výměna
- **ConceptMap**: chemická reakce, reaktant, produkt, katalyzátor, aktivační energie

### Chemické výpočty
- **FormulaPlayground**: n = m/M (molární hmotnost); c = n/V (koncentrace); slider
- **StepBySolver**: Kolik gramů NaCl pro roztok 0,5 mol/l v 500 ml?
- **CrossSubject**: Chemické výpočty → Mol → Prvky; výpočty → Stechiometrie → Neutralizace
- **ConceptMap**: mol, molární hmotnost, látková koncentrace, molární objem

### Chemická vazba
- **AiInteractiveDemo**: Vizualizace kovalentní vazby (sdílení elektronů), iontové, kovové
- **CrossSubject**: Chemická vazba → Ionty; iontová vazba → Elektrolýza; polarita → Voda
- **ComparisonTable**: Kovalentní vs. iontová vs. kovová vazba
- **ConceptMap**: chemická vazba, kovalentní, iontová, kovová, polarita

### Neutralizace
- **FormulaPlayground**: kyselina + zásada → sůl + voda; pH slider
- **AiInteractiveDemo**: Animace neutralizace HCl + NaOH; změna pH (indikátor)
- **CrossSubject**: Neutralizace → Kyseliny → Hydroxidy → Soli; pH → Trávení (biologie)
- **ComparisonTable**: Silná vs. slabá kyselina; neutralizace vs. redoxní reakce
- **ConceptMap**: neutralizace, kyselina, zásada, sůl, pH, indikátor

### Redoxní reakce
- **AiInteractiveDemo**: Animace přenosu elektronů — Fe⁰ + Cu²⁺ → Cu⁰ + Fe²⁺; výroba železa ve vysoké peci
- **CrossSubject**: Redoxní reakce → Galvanický článek → Elektrolýza; koroze → Soli
- **ComparisonTable**: Oxidace vs. redukce; galvanický článek vs. elektrolýza
- **ConceptMap**: oxidace, redukce, elektronový přenos, elektrochemická řada, koroze

### Názvosloví (halogeny, oxidy, sulfidy, hydroxidy, kyseliny, soli)
- **StepBySolver**: Odvození vzorce z názvu a názvu ze vzorce pro každý typ sloučeniny
- **AiInteractiveDemo**: Interaktivní názvosloví — zadej vzorec → AI pojmenuje; nebo název → AI napíše vzorec
- **CrossSubject**: Názvosloví → Vlastnosti látek; halogenidy → Ionty; kyseliny → Neutralizace
- **ConceptMap**: oxidační číslo, přípony -id/-an/-itý/-ičitý, vzorec, název

### Směsi
- **AiInteractiveDemo**: Vizualizace oddělování složek — filtrace, destilace, centrifugace
- **CrossSubject**: Směsi → Vzduch; směsi → Voda; suspenze → Krev (biologie)
- **ComparisonTable**: Homogenní (roztok) vs. heterogenní (suspenze/emulze)
- **ConceptMap**: směs, homogenní, heterogenní, roztok, suspenze, emulze

### Vzduch
- **FormulaPlayground**: Složení vzduchu: 78% N₂, 21% O₂, 1% Ar+CO₂; slider
- **AiInteractiveDemo**: Vizualizace složení vzduchu jako koláčový graf; pohyb molekul v atmosféře
- **CrossSubject**: Vzduch → Globální změny klimatu; vzduch → Dýchací soustava; vzduch → Akustika
- **ConceptMap**: vzduch, dusík, kyslík, argon, atmosféra, CO₂

### Voda
- **AiInteractiveDemo**: 3D vizualizace molekuly H₂O; animace vodíkových můstků; koloběh vody
- **CrossSubject**: Voda → Vodstvo ČR; voda → Vylučovací soustava; voda → Skupenství (fyzika)
- **ComparisonTable**: Slaná voda vs. sladká voda; povrchová vs. podzemní voda
- **ConceptMap**: voda, H₂O, hydrosféra, vodíkový můstek, skupenství

### Oxidy
- **AiInteractiveDemo**: Vizualizace struktury CO₂, SO₂; vliv CO₂ na skleníkový efekt
- **CrossSubject**: Oxidy → Názvosloví oxidů; CO₂ → Globální oteplování; SO₂ → Kyselé deště
- **ComparisonTable**: Kyselé oxidy (CO₂, SO₂) vs. zásadické oxidy (CaO, Na₂O)
- **ConceptMap**: oxid, oxidační číslo, CO₂, SO₂, kyselý déšť

### Kyseliny
- **AiInteractiveDemo**: Animace disociace HCl v roztoku → H⁺ + Cl⁻; pH indikátor
- **CrossSubject**: Kyseliny → Hydroxidy → Neutralizace → Soli; kyselina solná → Trávení (biologie)
- **ComparisonTable**: Kyslíkaté kyseliny vs. bezkyslíkaté; silná vs. slabá kyselina
- **ConceptMap**: kyselina, H⁺, pH, kyselé prostředí, disociace

### Hydroxidy
- **AiInteractiveDemo**: Animace disociace NaOH → Na⁺ + OH⁻
- **CrossSubject**: Hydroxidy → Neutralizace; Ca(OH)₂ → Stavebnictví
- **ComparisonTable**: NaOH vs. KOH vs. Ca(OH)₂ — vlastnosti a použití
- **ConceptMap**: hydroxid, OH⁻, zásada, bazické prostředí, mýdlo

### Soli
- **AiInteractiveDemo**: Animace vzniku NaCl iontovou vazbou; vznik soli neutralizací
- **CrossSubject**: Soli → Neutralizace; soli → Ionty; soli → Elektrolýza
- **ConceptMap**: sůl, kation, anion, neutrální, dusičnan, uhličitan, síran

### Elektrolýza a galvanický článek
- **AiInteractiveDemo**: Animace elektrolýzy ZnI₂ — pohyb iontů k elektrodám; schéma galvanického článku
- **CrossSubject**: Elektrolýza → Ionty; galvanický článek → Elektrický proud (fyzika)
- **ComparisonTable**: Galvanický článek vs. elektrolýza — spontánní vs. nucená reakce
- **ConceptMap**: elektrolýza, katoda, anoda, galvanický článek, akumulátor

### Organická chemie (základy, uhlovodíky, deriváty)
- **AiInteractiveDemo**: 3D vizualizace struktury metanu, etylenu, benzenu; animace polymerizace
- **StepBySolver**: Pojmenování alkanů; deriváty — methanol, ethanol
- **CrossSubject**: Uhlovodíky → Paliva → Elektrárny; alkoholy → Metabolismus (biologie); plasty → Globální problémy
- **ComparisonTable**: Alkany vs. alkeny vs. alkyny; nasycené vs. nenasycené uhlovodíky
- **ConceptMap**: uhlík, vodík, uhlovodík, alkan, alken, benzen, polymer

---

## BIOLOGIE — Lidské tělo

### O člověku
- **AiInteractiveDemo**: Interaktivní schéma lidského těla — klikni na orgán → základní informace
- **HistoryTimeline**: Homo habilis (2 mil. let) → Homo erectus → Homo sapiens (300 000 let) → dnes
- **CrossSubject**: Člověk → Genetika (DNA, evoluce); člověk → Biologie živočichů
- **ConceptMap**: Homo sapiens, bipedismus, mozek, evoluce, primáti

### Vylučovací soustava
- **AiInteractiveDemo**: Animace filtrace krve v ledvinách; schéma ledviny s glomerulem
- **CrossSubject**: Vylučovací soustava → Oběhová soustava; ionty v moči; osmóza → Tlak (fyzika/chemie)
- **ComparisonTable**: Ledviny vs. plíce vs. kůže — způsoby vylučování
- **ConceptMap**: ledvina, moč, filtrování, močový měchýř, nefron, dialýza

### Smyslová soustava
- **AiInteractiveDemo**: Interaktivní oko — klikni na části (rohovka, čočka, sítnice) → funkce; animace sluchu
- **CrossSubject**: Smyslová soustava → Světlo a optika (oko jako čočka); slyšení → Akustika; čich → Chemie (molekuly vůně)
- **ComparisonTable**: Oko vs. ucho — fyzikální principy vnímání
- **ConceptMap**: oko, ucho, sítnice, čočka, frekvence zvuku, smyslový receptor

### Kůže
- **AiInteractiveDemo**: Interaktivní řez kůží — klikni na vrstvu → funkce
- **CrossSubject**: Kůže → Teplota (fyzika — termoregulace); kůže → Nervová soustava (receptory dotyku)
- **ComparisonTable**: Pokožka vs. škára vs. podkoží
- **ConceptMap**: kůže, pokožka, škára, podkoží, termoregulace, keratin

### Opěrná soustava
- **AiInteractiveDemo**: Interaktivní lidská kostra — klikni na kost → název, typ, funkce
- **CrossSubject**: Opěrná soustava → Svalová soustava; kosti → Minerály (Ca, P); lebka → Mozek
- **ComparisonTable**: Dlouhé kosti vs. ploché vs. krátké; pevné spojení vs. kloub
- **ConceptMap**: kostra, kost, chrupavka, kloub, vazivo, kostní dřeň

### Svalová soustava
- **AiInteractiveDemo**: Animace svalové kontrakce (myosin/aktin); interaktivní mapa svalů těla
- **CrossSubject**: Svalová soustava → Opěrná soustava; svaly → Nervová soustava (ovládání)
- **ComparisonTable**: Kosterní sval vs. hladký sval vs. srdeční sval
- **ConceptMap**: sval, kontrakce, kosterní, hladký, srdeční, svalové vlákno

### Oběhová soustava
- **AiInteractiveDemo**: Animace průtoku krve srdcem; velký/malý krevní oběh s animovanými buňkami
- **CrossSubject**: Oběhová soustava → Dýchací soustava (O₂/CO₂ přenos); krev → Ionty; krevní skupiny → Genetika
- **ComparisonTable**: Červené krvinky vs. bílé krvinky vs. krevní destičky
- **ConceptMap**: srdce, krev, cévy, krevní oběh, erytrocyty, krevní skupiny

### Dýchací soustava
- **AiInteractiveDemo**: Animace vdechu a výdechu; výměna plynů v alveolech
- **CrossSubject**: Dýchací soustava → Vzduch (chemie — 21% O₂); dýchání → Glukóza → Energie
- **ComparisonTable**: Nos vs. ústa — filtrování vzduchu; alveoly vs. průdušky
- **ConceptMap**: plíce, alveoly, průdušky, O₂/CO₂, dýchání, bránice

### Nervová soustava
- **AiInteractiveDemo**: Animace nervového impulsu (akční potenciál); schéma reflexního oblouku
- **CrossSubject**: Nervová soustava → Hormonální soustava; smyslové orgány → Nervové signály
- **ComparisonTable**: Centrální nervová soustava vs. periferní; podmíněný reflex vs. nepodmíněný
- **ConceptMap**: neuron, mozek, mícha, reflex, synapsa, CNS

### Hormonální soustava
- **AiInteractiveDemo**: Schéma endokrinního systému — klikni na žlázu → hormon + funkce
- **CrossSubject**: Hormonální soustava → Nervová soustava; inzulín → Cukrovka; hypofýza → Růst
- **ComparisonTable**: Nervová regulace vs. hormonální regulace — rychlost a trvání
- **ConceptMap**: hormon, žláza, hypofýza, inzulín, adrenalin, štítná žláza

### Tkáně
- **AiInteractiveDemo**: Mikroskopický pohled na různé typy tkání
- **CrossSubject**: Tkáně → Opěrná soustava (pojivová); nervová → Nervová soustava; svalová → Svalová soustava
- **ComparisonTable**: Epitelová vs. pojivová vs. svalová vs. nervová tkáň
- **ConceptMap**: tkáň, buňka, epitel, pojivo, svalová, nervová

---

## BIOLOGIE — Genetika a vývoj

### Genetika
- **AiInteractiveDemo**: Simulace Mendelových zákonů — monohybridní křížení s interaktivním Punnetovým čtvercem
- **HistoryTimeline**: 1866 Mendel → 1953 Watson+Crick (DNA dvoušroubovice) → 2003 sekvenování lidského genomu
- **CrossSubject**: Genetika → DNA → Chemická vazba; dědičnost → Vývoj života; krevní skupiny → Oběhová soustava
- **ConceptMap**: DNA, gen, chromozom, dědičnost, dominantní, recesivní, mutace

### Vývoj života na Zemi
- **HistoryTimeline**: Prahory (bakterie) → Prvohory (ryby) → Druhohory (dinosauři) → Třetihory (savci) → Čtvrtohory (člověk)
- **GeoMap**: Mapa Pangey a pohyb kontinentů; místa klíčových fosilních nálezů
- **CrossSubject**: Vývoj života → Geologický vývoj ČR; evoluce → Genetika; fosílie → Horniny
- **ConceptMap**: evoluce, přirozený výběr, fosílie, geologická éra, Darwin

### Geologický vývoj ČR
- **GeoMap**: Mapa ČR s geologickými celky — Český masiv a Západní Karpaty
- **HistoryTimeline**: Variské vrásnění → Alpínské vrásnění → Čtvrtohory (zalednění) → současnost
- **CrossSubject**: Geologický vývoj → Stavba Země; geologické celky → Geomorfologie ČR
- **ConceptMap**: Český masiv, Karpaty, vrásnění, geologické období, horniny

---

## GEOGRAFIE — Česká republika

### Geomorfologie České republiky
- **GeoMap**: Mapa ČR s pohoří, nížinami, řekami — piny pro každou geomorfologickou jednotku
- **CrossSubject**: Geomorfologie → Hory a pohoří; geomorfologie → Vodstvo ČR; geomorfologie → Geologický vývoj
- **ComparisonTable**: Česká vysočina vs. Krkonošsko-jesenická soustava vs. Šumava
- **ConceptMap**: geomorfologie, pohoří, nížina, reliéf, Česká vysočina

### Základní informace o ČR
- **GeoMap**: Mapa ČR s hranicemi, sousedními státy, hlavním městem, krajskými centry
- **CrossSubject**: ČR → Hospodářství Česka; ČR → Obyvatelstvo Česka; ČR → Podnebí Česka
- **ConceptMap**: Česká republika, Praha, kraj, EU, NATO, rozloha

### Vodstvo a vodní plochy
- **GeoMap**: Hlavní řeky (Labe, Vltava, Morava, Odra) a přehrady; rozvodí Labe/Dunaje
- **CrossSubject**: Vodstvo → Voda (chemie); řeky → Geomorfologie; přehrady → Elektrárny (vodní)
- **ComparisonTable**: Labe vs. Vltava vs. Morava — délka, povodí, ústí
- **ConceptMap**: řeka, povodí, přehrada, rybník, rozvodí

### Nížiny, úvaly a pánve
- **GeoMap**: Polabská nížina, Jihomoravská nížina, Třeboňská pánev
- **CrossSubject**: Nížiny → Zemědělství (Hospodářství Česka); pánve → Geomorfologie
- **ConceptMap**: nížina, úval, pánev, Polabská nížina, reliéf

### Hory a pohoří
- **GeoMap**: Krkonoše (Sněžka), Šumava, Krušné hory, Beskydy, Jeseníky
- **CrossSubject**: Hory → Turistika; Sněžka → Podnebí; Šumava → Ochrana přírody
- **ComparisonTable**: Krkonoše vs. Šumava vs. Beskydy — výška, poloha, ochrana
- **ConceptMap**: pohoří, nadmořská výška, Sněžka, Šumava, Krkonoše

### Ochrana přírody v ČR
- **GeoMap**: Národní parky (Šumava, Krkonoše, Podyjí, České Švýcarsko) a CHKO
- **CrossSubject**: Ochrana přírody → Globální problémy; příroda → Vývoj života
- **ComparisonTable**: Národní park vs. CHKO vs. přírodní rezervace — stupeň ochrany
- **ConceptMap**: národní park, CHKO, chráněné území, biodiverzita, ekosystém

### Půdní fond ČR
- **GeoMap**: Mapa typů půd v ČR; oblasti orné půdy vs. lesů
- **CrossSubject**: Půdní fond → Zemědělství; eroze → Vnější geologické děje; hnojiva → Dusík (chemie)
- **ConceptMap**: půdní fond, orná půda, ZPF, eroze, lesní plochy

### ČR regiony — 14 krajů + Praha
- **GeoMap**: Mapa ČR s kraji — klikni na kraj → základní informace, pamětihodnosti, průmysl
- **HistoryTimeline**: Historický vývoj krajů; vznik krajského zřízení 2001
- **CrossSubject**: Regiony → Hospodářství Česka; kraje → Obyvatelstvo Česka
- **ComparisonTable**: Karlovarský kraj (lázně) vs. Moravskoslezský (průmysl); Praha vs. Vysočina — hustota

Vybrané kraje se silnou doporučenou interakcí:

**Středočeský kraj**
- **GeoMap**: Praha v centru, hrady (Karlštejn, Křivoklát), přehrady (Slapy, Orlík)

**Karlovarský kraj**
- **GeoMap**: Karlovy Vary, Mariánské Lázně, Františkovy Lázně; Krušné hory
- **ComparisonTable**: Termální prameny Karlových Varů vs. Mariánských Lázní

**Jihomoravský kraj**
- **GeoMap**: Brno, vinařské oblasti, Moravský kras (Macocha), hranice s Rakouskem/Slovenskem
- **ComparisonTable**: Vinařství Jihomoravský kraj vs. Zlínský kraj

**Hlavní město Praha**
- **GeoMap**: Historická centra — Staré Město, Hradčany, Malá Strana, Nové Město; UNESCO
- **HistoryTimeline**: Přemyslovci → Karel IV. → Habsburkové → 1918 Czechoslovakia → 1989 → EU

### Hospodářství Česka
- **GeoMap**: Průmyslové regiony — automobilky (Mladá Boleslav, Nošovice), energetika, zemědělské oblasti
- **CrossSubject**: Hospodářství → Sekundér; hospodářství → Obyvatelstvo Česka; průmysl → Elektrárny
- **ComparisonTable**: Primární sektor vs. sekundární vs. terciární v ČR
- **ConceptMap**: hospodářství, průmysl, zemědělství, HDP, automobilový průmysl

### Podnebí Česka
- **GeoMap**: Klimatická mapa ČR — přechod oceánské/kontinentální klima; srážková mapa
- **CrossSubject**: Podnebí → Globální změny klimatu; podnebí → Zemědělství; teplota → Fyzikální veličiny
- **ComparisonTable**: Oceánské klima vs. kontinentální klima — znaky v ČR
- **ConceptMap**: podnebí, klima, srážky, teplota, geografická šířka, nadmořská výška

### Obyvatelstvo Česka
- **GeoMap**: Mapa hustoty obyvatelstva v ČR; národnostní oblasti
- **CrossSubject**: Obyvatelstvo → Pohyb a migrace; porodnost → Demografická revoluce
- **ComparisonTable**: Přirozený přírůstek vs. mechanický; porodnost vs. úmrtnost
- **ConceptMap**: demografie, porodnost, natalita, migrace, národnost, hustota

---

## GEOGRAFIE — Svět

### Obyvatelstvo světa
- **GeoMap**: Kartogram hustoty světa; nejlidnatější státy (Čína, Indie, USA)
- **HistoryTimeline**: 1 miliarda (1800) → 2 miliardy (1927) → 5 miliard (1987) → 8 miliard (2022)
- **CrossSubject**: Obyvatelstvo → Globální problémy; migrace → Pohyb a migrace
- **ConceptMap**: demografie, hustota, demografická revoluce, přirozený přírůstek

### Pohyb a migrace
- **GeoMap**: Mapa světových migračních proudů; uprchlické trasy do Evropy
- **CrossSubject**: Migrace → Globální problémy; migrace → Obyvatelstvo Česka
- **ComparisonTable**: Přirozený pohyb vs. mechanický; emigrace vs. imigrace
- **ConceptMap**: migrace, emigrace, imigrace, přirozený přírůstek, asyl

### Rasy
- **GeoMap**: Mapa původu a rozšíření lidských ras; migrační trasy z Afriky
- **ComparisonTable**: Europoidní vs. mongoloidní vs. negroidní rasa — geografické rozšíření
- **CrossSubject**: Rasy → Genetika (93% shodná DNA); rasy → Vývoj člověka
- **ConceptMap**: rasa, Homo sapiens, antropologie, genetická diverzita

### Náboženství
- **GeoMap**: Mapa světových náboženství — rozšíření křesťanství, islámu, buddhismu, hinduismu
- **HistoryTimeline**: Judaismus (2000 př.n.l.) → Buddhismus (5. stol. př.n.l.) → Křesťanství (1. stol.) → Islám (7. stol.)
- **CrossSubject**: Náboženství → Středověká literatura (Bible); náboženství → Husité
- **ComparisonTable**: Monoteistická vs. polyteistická náboženství
- **ConceptMap**: náboženství, křesťanství, islám, buddhismus, judaismus, hinduismus

### Globalizace
- **GeoMap**: Mapa světových obchodních tras; globální dodavatelské řetězce
- **AiInteractiveDemo**: Vizualizace globálního propojení — odkud pochází jeden produkt (iPhone — 50+ zemí)
- **CrossSubject**: Globalizace → Globální problémy; globalizace → Hospodářství světa
- **ComparisonTable**: Výhody globalizace vs. nevýhody; rozvinuté vs. rozvojové země
- **ConceptMap**: globalizace, obchod, nadnárodní společnost, internet, Green Deal

### Globální změny klimatu
- **GeoMap**: Mapa oblastí ohrožených klimatickými změnami; ozonová díra nad Antarktidou
- **AiInteractiveDemo**: Simulace skleníkového efektu — slider pro CO₂ → změna teploty; tání ledovců
- **CrossSubject**: Klimatické změny → CO₂ (chemie); klima → Podnebí Česka; solární panely → Elektrárny
- **ComparisonTable**: Obnovitelné vs. neobnovitelné zdroje — vliv na klima
- **ConceptMap**: skleníkový efekt, CO₂, globální oteplování, ozonosféra, uhlíková stopa

### Globální problémy
- **GeoMap**: Mapa chudoby světa; oblasti s nedostatkem vody; válečné konflikty
- **HistoryTimeline**: Prognózy vědců 2030 → 2040 → 2050 → 2080 → 2100
- **CrossSubject**: Globální problémy → Klimatické změny; chudoba → Hospodářství světa
- **ConceptMap**: chudoba, klimatické změny, přelidnění, negramotnost

### Hospodářství světa (primér, sekundér, terciér)
- **GeoMap**: Zemědělské oblasti světa; průmyslové regiony
- **HistoryTimeline**: 1. průmyslová revoluce (parní stroj 1769) → 2. (elektřina) → 3. (IT) → 4. (AI, roboti)
- **CrossSubject**: Sekundér → Elektrárny; průmyslová revoluce → Chemie (výroba oceli); zemědělství → Půdní fond
- **ComparisonTable**: Primér vs. sekundér vs. terciér — podíl na HDP ve vybraných zemích
- **ConceptMap**: primér, sekundér, terciér, průmyslová revoluce, HDP, zemědělství

---

## ASTRONOMIE

### Měsíc
- **GeoMap**: Mapa Měsíce — moře, krátery, Apollo přistání
- **AiInteractiveDemo**: Interaktivní animace fází Měsíce; vizualizace přílivu a odlivu
- **HistoryTimeline**: 1609 Galileo (dalekohled) → 1959 Luna 1 → 1969 Apollo 11 → 2024 Artemis program
- **CrossSubject**: Měsíc → Světlo a optika (odraz slunečního světla); fáze → Gravitace (fyzika)
- **ConceptMap**: Měsíc, fáze, příliv, odliv, gravitace, Apollo, krátery

### Umělé družice
- **GeoMap**: Mapa satelitních oběžných drah; vizualizace ISS dráhy
- **AiInteractiveDemo**: Simulace oběhu satelitu — slider pro výšku dráhy → orbitální rychlost
- **HistoryTimeline**: 1957 Sputnik 1 → 1961 Gagarin → 1969 Apollo → 1998 ISS
- **CrossSubject**: Družice → Elektromagnetismus (GPS signál); meteorologické družice → Podnebí
- **ComparisonTable**: Spojovací vs. navigační vs. průzkumné vs. vojenské satelity
- **ConceptMap**: satelit, oběžná dráha, ISS, GPS, telekomunikace

### Slunce
- **AiInteractiveDemo**: Vizualizace slunečního systému; animace solárního větru a magnetického pole Země
- **CrossSubject**: Slunce → Světlo a optika; solární energie → Elektrárny; sluneční skvrny → Klima
- **ComparisonTable**: Slunce vs. jiné typy hvězd (velikost, teplota, barva)
- **ConceptMap**: Slunce, hvězda, sluneční vítr, fotosféra, solární erupce, fotosyntéza

### Hvězdy
- **AiInteractiveDemo**: Interaktivní HR diagram (Hertzsprung-Russell); animace životního cyklu hvězdy → supernova
- **HistoryTimeline**: Big Bang → vznik první hvězdy → naše Slunce → budoucí smrt Slunce (5 mld. let)
- **CrossSubject**: Hvězdy → Radioaktivita (jaderná fúze); hvězdy → Prvky (vznik těžkých prvků v hvězdách)
- **ComparisonTable**: Bílý trpaslík vs. červený obr vs. neutronová hvězda
- **ConceptMap**: hvězda, supernova, jaderná fúze, spektrální třída, HR diagram

---

## GEOLOGIE

### Minerály (sulfidy, halogenidy, oxidy, uhličitany, dusičnany, sírany)
- **GeoMap**: Mapa výskytu minerálů v ČR a ve světě — zlatá ložiska, sulfidická ložiska
- **AiInteractiveDemo**: Interaktivní mineralogická encyklopedie — klikni na minerál → vlastnosti (tvrdost, lesk, štěpnost)
- **CrossSubject**: Minerály → Horniny; minerály → Chemie (oxidy, sulfidy); kalcit → Vápník (kosti)
- **ComparisonTable**: Sulfidy vs. oxidy vs. uhličitany — složení a výskyt
- **ConceptMap**: minerál, tvrdost (Mohsova stupnice), lesk, sulfid, halogenid

### Horniny (základy, vyvřelé, usazené, přeměněné)
- **GeoMap**: Mapa výskytu hornin v ČR — žula (Šumava/Krkonoše), čedič (severní Čechy)
- **AiInteractiveDemo**: Horninový cyklus — animace přechodu vyvřelé → usazené → přeměněné
- **CrossSubject**: Horniny → Stavba Země; horniny → Vnitřní/vnější geologické děje; žula → Těžba
- **ComparisonTable**: Vyvřelé (žula/čedič) vs. usazené (vápenec/pískovec) vs. přeměněné (mramor/rula)
- **ConceptMap**: hornina, magma, žula, čedič, vápenec, mramor, horninový cyklus

### Stavba Země
- **AiInteractiveDemo**: Interaktivní řez Zemí — klikni na vrstvu → teplota, složení, tloušťka
- **CrossSubject**: Stavba Země → Litosférické desky; stavba → Vnitřní geologické děje
- **ComparisonTable**: Kontinentální kůra vs. oceánská kůra; vnější jádro vs. vnitřní jádro
- **ConceptMap**: zemská kůra, plášť, jádro, litosféra, astenosféra, magma

### Litosférické desky
- **GeoMap**: Mapa světových litosférických desek; ohniska zemětřesení a sopečná činnost (Pacifický ohnivý kruh)
- **AiInteractiveDemo**: Animace pohybu desek — odsouvání, podsouvání, srážení; vznik pohoří
- **CrossSubject**: Litosférické desky → Vnitřní geologické děje; tektonika → Sopky → Vyvřelé horniny
- **ComparisonTable**: Odsouvání (rifting) vs. podsouvání (subdukce) vs. srážení (kolize)
- **ConceptMap**: litosférická deska, tektonika, subdukce, zemětřesení, sopka

### Vnitřní geologické děje
- **GeoMap**: Mapa zemětřesení světa; aktivní sopky; Yellowstone, Vesuv, Etna
- **AiInteractiveDemo**: Animace sopky — magma → kráter; seismograf; ohnisko a epicentrum
- **HistoryTimeline**: 79 n.l. Vesuv → 1815 Tambora → 1883 Krakatoa → 1980 Mt. St. Helens → 2010 Island
- **CrossSubject**: Sopky → Horniny (vyvřelé); zemětřesení → Litosférické desky; tsunami → Kapaliny (fyzika)
- **ComparisonTable**: Stratovulkán vs. štítová sopka
- **ConceptMap**: zemětřesení, epicentrum, ohnisko, Richterova stupnice, sopka, magma

### Vnější geologické děje
- **AiInteractiveDemo**: Animace zvětrávání, eroze, transportu a sedimentace; vznik kaňonu
- **CrossSubject**: Vnější geologické děje → Usazené horniny; eroze → Půdní fond ČR; řeky → Vodstvo ČR
- **ComparisonTable**: Mechanické zvětrávání vs. chemické vs. biologické zvětrávání
- **ConceptMap**: zvětrávání, eroze, sedimentace, říční síť, eolická eroze

---

## HUDBA — Teorie

### Takty a předznamenání
- **AiInteractiveDemo**: Interaktivní notová osnova — klikni na takt a uslyš jeho zvuk; vizualizace 2/4, 3/4, 4/4
- **ComparisonTable**: 2/4 vs. 3/4 vs. 4/4 takt — počet dob, charakter
- **CrossSubject**: Takty → Délky not; takty → Taktování; předznamenání → Jména not
- **ConceptMap**: takt, taktová čára, předznamenání, křížek, béčko

### Jména not
- **AiInteractiveDemo**: Interaktivní klávesnice nebo notová osnova — klikni na notu → přehraj zvuk
- **CrossSubject**: Jména not → Složení noty; noty → Délky not; výška tónu → Akustika (frekvence)
- **ConceptMap**: nota, tón, C, D, E, F, G, A, H, oktáva

### Složení noty
- **AiInteractiveDemo**: Interaktivní nota — slider pro délku; přehrej zvuk
- **ComparisonTable**: Celá → půlová → čtvrťová → osminová nota — délky a výzor
- **ConceptMap**: nota, hlavička, nožka, praporek, trámec, délka noty

### Polyfonie a homofonie
- **AiInteractiveDemo**: Přehrávač — slyš příklad polyfonie (fuga) vs. homofonie (píseň s doprovodem)
- **ComparisonTable**: Polyfonie vs. homofonie — hlasy, rovnocennost, příklady
- **CrossSubject**: Polyfonie → Baroko (J.S. Bach); homofonie → Klasicismus
- **ConceptMap**: polyfonie, homofonie, hlas, melodie, harmonie, fuga

### Znaménka síly (dynamika)
- **AiInteractiveDemo**: Posuvník hlasitosti — přehrej ukázku v pianissimo, forte, fortissimo; crescendo
- **ComparisonTable**: Piano vs. forte; crescendo vs. diminuendo
- **ConceptMap**: dynamika, piano, forte, crescendo, diminuendo, pianissimo

### Zpěvní hlasy
- **AiInteractiveDemo**: Přehrávač ukázek každého hlasu — soprán, alt, tenor, bas; rozsah na klaviatuře
- **ComparisonTable**: Ženské hlasy (soprán/alt) vs. mužské (tenor/bas) — rozsah
- **ConceptMap**: soprán, mezzosoprán, alt, tenor, baryton, bas

### Délky not a pomlky
- **AiInteractiveDemo**: Interaktivní rytmická cvičení — klikni na notu → přehraj rytmus
- **ComparisonTable**: Celá vs. půlová vs. čtvrťová vs. osminová → počet dob
- **ConceptMap**: nota, pomlka, doba, celá, půlová, čtvrťová, osminová

### Taktování a dirigování
- **AiInteractiveDemo**: Animace taktovacích schémat pro 2/4, 3/4, 4/4 — pohybující se taktovka
- **ConceptMap**: dirigent, taktování, taktovka, schéma, tempo, beat

---

## HUDBA — Nástroje a formy

### Orchestrální / symfonické / lidové nástroje
- **AiInteractiveDemo**: Interaktivní schéma symfonického orchestru — klikni na nástroj → přehraj zvuk + info
- **ComparisonTable**: Smyčcové vs. dechové vs. bicí vs. klávesové — způsob tvorby zvuku
- **CrossSubject**: Nástroje → Akustika (fyzika — rezonance, frekvence); varhany → Vzduch (tlak)
- **ConceptMap**: symfonický orchestr, smyčcové, dechové, bicí, klávesové, cimbál

### Hudební formy (symfonie, kantáta, oratorium, muzikál, sonáta, symfonická báseň)
- **AiInteractiveDemo**: Přehrávač ukázek každé formy s anotovaným průvodcem — slyš a sleduj strukturu
- **HistoryTimeline**: Renesance (madrigal) → Baroko (kantáta, oratorium) → Klasicismus (symfonie) → Romantismus (symfonická báseň) → Muzikál (20. stol.)
- **ComparisonTable**: Symfonie vs. symfonická báseň; kantáta vs. oratorium
- **CrossSubject**: Hudební formy → Skladatelé (Beethoven, Bach, Smetana, Händel)
- **ConceptMap**: symfonie, věta, sonátová forma, kantáta, oratorium, muzikál

---

## HUDBA — Skladatelé

### Antonín Dvořák
- **GeoMap**: Nelahozeves (rodiště), Praha (varhanická škola), New York (Novosvětská), Vysoká u Příbramě
- **HistoryTimeline**: 1841 narozen → 1857 Praha → 1875 státní stipendium → 1892–95 New York → 1904 zemřel
- **CrossSubject**: Dvořák → Smetana (Prozatímní divadlo); Dvořák → Romantismus v literatuře
- **ConceptMap**: Dvořák, Novosvětská symfonie, Rusalka, Slovanské tance, New York

### Bedřich Smetana
- **GeoMap**: Litomyšl (rodiště), Praha (Národní divadlo), Jabkenice
- **HistoryTimeline**: 1824 Litomyšl → 1866 Prodaná nevěsta → 1874 hluchota → 1879 Má vlast → 1884 zemřel
- **CrossSubject**: Smetana → Vltava (vodstvo ČR); Má vlast → Geomorfologie ČR; Smetana → Dvořák, Janáček
- **ConceptMap**: Smetana, Má vlast, Prodaná nevěsta, Vltava, Vyšehrad, symfonická báseň

### Leoš Janáček
- **GeoMap**: Hukvaldy (dětství, Lašsko), Brno (varhanická škola), Brno (opery)
- **HistoryTimeline**: 1854 narozen → 1881 Brno → sběr lidových písní → 1904 Její pastorkyňa → 1928 zemřel
- **CrossSubject**: Janáček → Lidové nástroje; Janáček → Moravský folklor; nápěvky řeči → Akustika
- **ConceptMap**: Janáček, Její pastorkyňa, Příhody lišky Bystroušky, Lašsko, lidové písně

### Fryderyk Chopin
- **GeoMap**: Polsko (rodiště), Paříž (kariéra), Mallorca (tuberkulóza)
- **HistoryTimeline**: 1810 narozen → 1830 odchod do Paříže → 1849 smrt (tuberkulóza)
- **CrossSubject**: Chopin → Romantismus; Polsko → Obyvatelstvo světa (migrace); klavír → Klávesové nástroje
- **ComparisonTable**: Mazurka vs. polonéza vs. etuda — charakter, takt, původ
- **ConceptMap**: Chopin, mazurka, polonéza, Romantismus, klavír, Polsko

---

## HUDBA — Populární hudba

### Rock & roll
- **GeoMap**: USA — Memphis (Elvis Presley), Cleveland (Rock Hall of Fame), Chicago (blues)
- **HistoryTimeline**: 1954 první nahrávky → Elvis (1956) → Beatles (1963) → Woodstock (1969)
- **CrossSubject**: Rock & roll → Blues; elektrická kytara → Elektřina (fyzika)
- **ComparisonTable**: Blues vs. rock & roll vs. jazz — rytmus, nástroje, nálada
- **ConceptMap**: rock & roll, Elvis Presley, Chuck Berry, elektrická kytara, blues

### Jazz
- **GeoMap**: New Orleans (vznik), Chicago, New York (Carnegie Hall, Harlem); Praha (Osvobozené divadlo)
- **HistoryTimeline**: 1900 New Orleans → 1920s Swing → 1940s Bebop → 1930s Praha (Osvobozené divadlo)
- **CrossSubject**: Jazz → Blues a spirituály; improvizace → Kreativita
- **ComparisonTable**: Pochodový jazz vs. klasický jazz vs. swing vs. bebop
- **ConceptMap**: jazz, improvizace, syncopace, swing, bebop, Louis Armstrong

### Písně (o vojně, milostné, blues, spirituály, pracovní)
- **AiInteractiveDemo**: Přehrávač ukázek každého žánru; vizualizace typických rysů (takt, tempo)
- **ComparisonTable**: Lidová píseň vs. umělá píseň; blues vs. spirituál
- **CrossSubject**: Blues/spirituály → Afroameričtí otroci → Obyvatelstvo světa (migrace)
- **ConceptMap**: lidová píseň, umělá píseň, blues, spirituál, triola

---

## DĚJINY HUDBY

### Historie hudby ve světě
- **HistoryTimeline**: Pravěk → Antické Řecko → Středověk → Renesance (opera) → Baroko (Bach) → Klasicismus (Mozart) → Romantismus → 20. stol. (jazz, rock)
- **CrossSubject**: Hudební dějiny → Dvořák, Smetana, Janáček; baroko → Barokní literatura
- **ConceptMap**: hudební periody, renesance, baroko, klasicismus, romantismus

### Historie hudby v českých zemích
- **HistoryTimeline**: Počátky → Karel IV. → Husité → Baroko → Klasicismus → Smetana → Dvořák → Janáček
- **GeoMap**: Litomyšl (Smetana), Nelahozeves (Dvořák), Hukvaldy (Janáček), Praha
- **CrossSubject**: Česká hudba → Literatura doby husitské; národní obrození → Preromantismus
- **ConceptMap**: česká hudba, Smetana, Dvořák, Janáček, národní obrození, baroko

---

## ČESKÝ JAZYK — Slohová výchova

### Vypravování
- **AiInteractiveDemo**: Generátor osnovy — AI navrhne úvod/zápletku/vyvrcholení/závěr na zadané téma
- **StepBySolver**: Krok za krokem — od osnovy po čistopis
- **ComparisonTable**: Prosté vypravování vs. umělecké vypravování
- **ConceptMap**: děj, napětí, přímá řeč, atmosféra, gradace, zápletek

### Úvaha
- **StepBySolver**: Jak formulovat tezi → argumenty → závěr
- **AiInteractiveDemo**: Zadej téma → AI vygeneruje argumenty pro a proti
- **ConceptMap**: teze, argument, závěr, hodnotový soud

### Charakteristika
- **AiInteractiveDemo**: Zadej jméno postavy → AI vygeneruje vzorovou charakteristiku; žák ji opraví
- **ComparisonTable**: Vnější charakteristika vs. vnitřní charakteristika
- **ConceptMap**: charakteristika, vlastnosti, chování, vnější/vnitřní popis

### Popis, Životopis, Líčení, Výklad, Výtah, Žádost, Zpráva a oznámení
- **AiInteractiveDemo**: AI generuje ukázkový text daného žánru → žák ho analyzuje nebo napodobuje
- **ComparisonTable**: Popis vs. Líčení; Zpráva vs. Oznámení; Výklad vs. Úvaha
- **ConceptMap**: pro každý žánr klíčové pojmy (životopis: chronologický přehled, CV; líčení: subjektivní, obrazné prostředky)

---

## ČESKÝ JAZYK — Mluvnice a pravopis

### Přídavná jména (stupňování, mluvnické kategorie, jmenné tvary)
- **AiInteractiveDemo**: Zadej adjektivum → AI ukáže všechny tvary a skloňování
- **StepBySolver**: Stupňování nepravidelných tvarů (dobrý → lepší → nejlepší)
- **ComparisonTable**: Pravidelné stupňování vs. nepravidelné; tvrdá adjektiva vs. měkká
- **ConceptMap**: adjektivum, rod, pád, číslo, stupeň, vzor

### Zájmena (jenž, druhy zájmen)
- **StepBySolver**: Skloňování zájmena "jenž" krok za krokem
- **AiInteractiveDemo**: Zadej větu → AI označí zájmeno a určí jeho druh
- **ComparisonTable**: Zájmeno "jenž" vs. "který" — kdy použít
- **ConceptMap**: zájmeno, jenž, který, vztažné, osobní, přivlastňovací

### Číslovky (dělení, psaní dva, oba)
- **AiInteractiveDemo**: Interaktivní cvičení — vyber správný tvar ve větě
- **StepBySolver**: Skloňování číslovky "dva" a "oba" v tabulce
- **ConceptMap**: číslovka, základní, řadová, druhová, násobná

### Slovesa (mluvnické kategorie, tvar jednoduchý a složený)
- **AiInteractiveDemo**: Zadej sloveso → AI zobrazí všechny tvary (osoba, číslo, čas, způsob, rod, vid)
- **StepBySolver**: Tvorba složených tvarů (byl bych četl); dokonavý vs. nedokonavý vid
- **ConceptMap**: sloveso, osoba, číslo, čas, způsob, rod, vid

### Příslovce (stupňování, základy)
- **AiInteractiveDemo**: Zadej příslovce → AI ukáže stupňování a způsob tvoření
- **ComparisonTable**: Pravidelné stupňování vs. nepravidelné (dobře → lépe → nejlépe)
- **ConceptMap**: příslovce, místo, čas, způsob, míra, stupňování

### Neohebné slovní druhy (předložky, částice, spojky, citoslovce)
- **AiInteractiveDemo**: Zadej větu → AI označí všechny neohebné slovní druhy
- **ComparisonTable**: Podřadicí spojky vs. souřadicí; vlastní předložky vs. nevlastní
- **ConceptMap**: předložka, spojka, částice, citoslovce

### Větné členy (podmět, přísudek, předmět, přívlastek, příslovečné určení, doplněk, přístavek)
- **AiInteractiveDemo**: Zadej větu → AI barevně označí větné členy (grafická analýza věty)
- **StepBySolver**: Jak najít podmět → přísudek → předmět krok za krokem
- **ComparisonTable**: Přívlastek shodný vs. neshodný; těsný vs. volný
- **ConceptMap**: podmět, přísudek, předmět, přívlastek, příslovečné určení

### Pravopis (velká písmena, vyjmenovaná slova, předpony s/z/vz, psaní ě, ú/ů)
- **AiInteractiveDemo**: Diktát s automatickým vyhodnocením chyb; cvičení na vyjmenovaná slova
- **StepBySolver**: Pravidla pro s- vs. z- vs. vz- s příklady; psaní bě/bje/pě/mě/mně
- **ConceptMap**: pravopis, vyjmenovaná slova, předpona, velká písmena, pád

### Nauka o slovní zásobě
- **AiInteractiveDemo**: Synonymní vyhledávač — zadej slovo → AI najde synonyma, antonyma, homonyma
- **ComparisonTable**: Synonyma vs. antonyma vs. homonyma
- **ConceptMap**: lexikologie, synonymum, antonymum, homonymum, přísloví, frazeologismus

### Obecné poučení o jazyce / jazykové rodiny / přechodníky
- **AiInteractiveDemo**: Mapa jazykových rodin v Evropě — klikni na jazyk → jazyková rodina, příbuzné jazyky
- **ComparisonTable**: Slovanské jazyky vs. germánské vs. románské; spisovná vs. nářečí vs. obecná čeština
- **StepBySolver**: Tvorba přechodníku přítomného a minulého krok za krokem
- **ConceptMap**: jazykové rodiny, slovanské jazyky, čeština, dialekt, přechodník

---

## ČESKÁ LITERATURA — Žánry

### Báje
- **GeoMap**: Mapa starověkého Řecka a Říma — Olymp, Mykény, Trója, Řím
- **HistoryTimeline**: Řecká mytologie → Římská mytologie → Eduard Petiška (20. stol. česká adaptace)
- **ComparisonTable**: Báje vs. pověst vs. pohádka — vztah k místu, nadpřirozenu, historii
- **ConceptMap**: mytologie, bůh, hrdina, polobůh, Olymp, Zeus, Prométheus

### Pověst
- **GeoMap**: Mapa ČR s místy pověstí — Blaník, Říp (praotec Čech), Vyšehrad, Krkonoše (Krakonoš)
- **HistoryTimeline**: Starověké pověsti → Středověké kroniky (Kosmas) → 19. stol. sběratelé (Alois Jirásek)
- **ComparisonTable**: Pověst vs. pohádka vs. legenda — realnost jádra
- **ConceptMap**: pověst, historické jádro, místní pověst, historická pověst, Alois Jirásek

### Drama
- **AiInteractiveDemo**: Interaktivní schéma divadelní hry — akty, scény, záhlaví, jeviště
- **ComparisonTable**: Tragédie vs. komedie vs. činohra; dialog vs. monolog
- **ConceptMap**: drama, dialog, monolog, komedie, tragédie, Molière

### Balada
- **ComparisonTable**: Balada vs. romance — nálada, konec, téma
- **ConceptMap**: balada, lyricko-epická báseň, tragický konec, Kytice, K. J. Erben

### Pohádka
- **ComparisonTable**: Lidová pohádka vs. autorská; česká pohádka vs. Bratři Grimmové
- **ConceptMap**: pohádka, nadpřirozené, dobro/zlo, poučení, Erben, Božena Němcová

---

## ČESKÁ LITERATURA — Dějiny literatury

### Středověká literatura
- **HistoryTimeline**: 5. stol. (pád Říma) → Velikonoční hry → Dante (1300) → Jan Hus (1415) → Gutenberg (1450)
- **GeoMap**: Mapa středověkých literárních center — Praha, Florencie (Dante), Londýn (Chaucer)
- **ConceptMap**: legenda, hagiografie, kroniky, rytířský román, trubadúr

### Renesance a humanismus
- **HistoryTimeline**: 1307 Dante → 1450 Gutenberg → 1492 Kolumbus → 1516 Cervantes → 1600 Shakespeare
- **GeoMap**: Mapa renesanční Evropy — Itálie, Španělsko, Anglie, Francie; Columbus → Nový svět
- **CrossSubject**: Renesance → Galileo (astronomie); tisck → Šíření vědy
- **ComparisonTable**: Renesance vs. baroko — hodnoty, styl, témata
- **ConceptMap**: renesance, humanismus, Dante, Petrarca, Cervantes, Shakespeare

### Romantismus
- **HistoryTimeline**: 1776 Goethe (Werther) → 1810 Mácha (narozen) → 1836 Máj → 1848 Revoluce
- **ComparisonTable**: Romantismus vs. klasicismus — hrdina, příroda, emoce, pravidla
- **ConceptMap**: romantismus, národní obrození, Mácha, Máj, výjimečný hrdina, příroda

### Antická literatura
- **GeoMap**: Mapa starověkého Řecka — Athény, Sparta, Trója, Olymp, Řím
- **HistoryTimeline**: 8. stol. př.n.l. Homér → 5. stol. Sofoklés → 1. stol. Vergilius → Ovidius
- **ConceptMap**: Homér, Ilias, Odyssea, Sofoklés, drama, Oidipus, Vergilius

### Gilgameš
- **GeoMap**: Mapa Mezopotámie — Uruk, Sumer, Babylonie; povodí Tigridu a Eufratu
- **HistoryTimeline**: ~2700 př.n.l. historický Gilgameš → 3. tisíciletí př.n.l. epos → objeven 1853 v Ninive
- **CrossSubject**: Gilgameš → Bible (podobnost s potopou světa); Mezopotámie → Starověké civilizace
- **ConceptMap**: Gilgameš, Enkidu, Uruk, Sumer, nesmrtelnost, potopa

### Realismus (světový)
- **HistoryTimeline**: 1830 počátek → Gogol → Dostojevskij → Tolstoj → Flaubert → Dickens → 1890 konec
- **GeoMap**: Rusko (Dostojevskij/Tolstoj), Francie (Balzac/Flaubert), Anglie (Dickens)
- **ComparisonTable**: Romantismus vs. realismus; ruský vs. francouzský vs. anglický realismus
- **ConceptMap**: realismus, objektivita, naturalismus, Dostojevskij, Tolstoj, Flaubert

### Česká próza 1. pol. 20. stol.
- **HistoryTimeline**: 1918 vznik ČSR → 1920s avantgarda → 1930s Čapek (R.U.R.) → 1939 okupace
- **CrossSubject**: Čapek → Roboti → Technologie; Válka s Mloky → ekologie → globální problémy
- **ConceptMap**: Čapek, Hašek, Švejk, R.U.R., poetismus, avantgarda, robot

---

## OBECNÉ — Jazyk a písmo

### Jazyky světa
- **GeoMap**: Mapa jazykových rodin světa — kde se mluví indoevropskými, sino-tibetskými, semitskými jazyky
- **CrossSubject**: Jazyky → Obyvatelstvo světa; jazyky → Česká jazykověda; jazyky → Náboženství (liturgické jazyky)
- **ComparisonTable**: Izolační (čínština) vs. flektivní (čeština, latina) vs. aglutinační (finština)
- **ConceptMap**: jazyková rodina, indoevropský jazyk, slovanský jazyk, dialekt, lingua franca

### Písmo
- **HistoryTimeline**: Piktogramy (30 000 př.n.l.) → Sumerské klínové písmo (3200 př.n.l.) → Egyptské hieroglyfy → Fénická abeceda (1050 př.n.l.) → Řecká abeceda → Latinská abeceda → Cyrilice → Tisk (1450)
- **GeoMap**: Mapa vzniku a šíření písem — Mezopotámie, Egypt, Fénicie, Řecko
- **CrossSubject**: Písmo → Tisk → Gutenberg → Renesance; písmo → Jazyky
- **ComparisonTable**: Ideografické písmo (čínské znaky) vs. alphabetické (latinské) vs. slabičné (japonská kana)
- **ConceptMap**: písmo, hieroglyfy, klínové písmo, abeceda, fénické písmo, cyrilice
