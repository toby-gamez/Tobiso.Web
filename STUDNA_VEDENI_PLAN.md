# Studna vědění – Plán redesignu Tobiso

> Cíl: Přeměnit Tobiso z kolekce stránek na propojenou síť znalostí – každý článek je uzel, který přirozeně vede k dalšímu obsahu napříč předměty.

---

## Přehled fází

| Fáze | Název | Náročnost | Stav |
|------|-------|-----------|------|
| 1 | Contextual Layer | Nízká | 🔲 Nezačato |
| 2 | Graf znalostí | Střední | 🔲 Nezačato |
| 3 | GeoContext – Mapa | Střední | 🔲 Nezačato |
| 4 | HistoryContext – Časová osa | Nízká | 🔲 Nezačato |
| 5 | Explorační mód | Vysoká | 🔲 Nezačato |

---

## Fáze 1 – Contextual Layer

**Co to je:** Fixní kontextová lišta zobrazující se v horní části každého článku. Propojuje existující funkce (mapa, osa, person, síť) do jednoho viditelného místa.

**Vizuální návrh:**
```
┌─────────────────────────────────────────────────────────────┐
│  [📍 Mapa]  [🕐 Časová osa]  [👤 Karel IV.]  [🔗 7 uzlů]  │
└─────────────────────────────────────────────────────────────┘
```

**Pravidla zobrazení:**
- Ikony se zobrazují jen pokud je relevantní obsah
- Mapa: zeměpis, dějepis s geolokací
- Časová osa: dějepis, fyzika/chemie s historickým kontextem
- Person AI: pokud má článek přiřazenou Person
- Uzly: vždy (počet propojených článků)

**Nové soubory:**
- `Tobiso.Web.App/Components/Shared/ArticleContextBar.razor`
- `Tobiso.Web.App/Components/Shared/ArticleContextBar.razor.css`

**Změny stávajících souborů:**
- `PostDetail.razor` – přidání `<ArticleContextBar>` komponentu pod breadcrumb

**Odhadovaná práce:** 3–4 dny

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
