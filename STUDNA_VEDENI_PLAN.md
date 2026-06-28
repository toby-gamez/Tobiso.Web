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
| 26 | Teacher assignments & confusion heatmap | 🔲 Not started |
| 1b | Exercises in sidebar (layout) | ✅ Done |
| 27 | Spaced Repetition & Téma dne | 🔲 Not started – requires accounts |

> **Note:** Fáze 1 contains only the split layout and buttons linking to existing modals. The actual Map, Timeline, and AI-tag Graph content for those buttons is built in Fáze 2–4.

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

**Odhadovaná práce:** 5–7 dní

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
| 2 – Graf znalostí | 5–7 |
| 3 – Mapa | 4–5 |
| 4 – Časová osa | 3–4 |
| 5 – Explorační mód | 5–6 |
| **Celkem** | **20–26 dní** |

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
- Výsledek cache-ovat v DB (PostCrossConnections tabulka)

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
| 2 – Graf znalostí | 5–7 |
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
