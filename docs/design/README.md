# Handoff: Tobiso.Web UI Redesign

## Implementation Status
This redesign has already been implemented in `Tobiso.Web.App` (Organic tokens live in `wwwroot/css/variables.css` + `wwwroot/css/organic.css`). Treat this folder as historical design reference, not as a description of a pending task. Known deviations from the text below:
- The app is **Blazor Server**, not Blazor WebAssembly as stated in this doc's Overview line — the doc was written against a stale premise.
- The markdown lead-paragraph wrapper is `class="post-lead"` in the shipped code, not `class="intro"` as described under Post detail.
- `Exercise Types.dc.html` (in this same folder) **supersedes** this doc's exercise interactions: native HTML5 drag-and-drop was replaced with a click-to-select-then-place model (click a chip, then click a bucket) for mobile-friendliness. Follow `Exercise Types.dc.html` for exercise UI, not the drag-drop assumptions elsewhere in this file.
- See `CLAUDE.md`'s Design System section for the current token summary and the rule for using this folder on new UI work.

## Overview
A full redesign of the Tobiso.Web public app (Blazor WebAssembly) — app shell, navigation, and the post/article renderer — grounded in `ARCHITECTURE.md` (commit `5577986`) from the `toby-gamez/Tobiso.Web` repo. Built on the **Organic** design system (warm cream ground, terracotta + sage accents, Baloo 2 heading / Figtree body, 16px→pill radii).

## About the Design Files
The files in this bundle (`Tobiso App.dc.html`, `Tobiso.dc.html`, `PostBody.dc.html`) are **design references built in HTML** — not production code. The task is to recreate these designs as Razor components inside the existing `Tobiso.Web.App` Blazor project, using its existing routing, CSS variable system (`wwwroot/css/variables.css`), and Markdown/KaTeX rendering pipeline (`MarkdownContent.razor`) — not to embed the HTML directly. Where a design introduces a new visual system (colors, type, radii), replace the values in `variables.css` accordingly; where it introduces new markup/structure, rebuild that structure as `.razor` markup.

## Fidelity
**High-fidelity.** Colors, type, spacing, radii and copy (in Czech) are final. Treat pixel values and CSS variable references below as the source of truth.

## What Changed vs. Today's App
- Replaced Bootstrap-icon + pink (`#d175a6`) palette with the Organic system: cream `--color-bg` (#f5ead8), terracotta accent ramp, sage second accent, Baloo 2 for all headings (chosen over the system's default Caprasimo, which lacks Czech diacritics — ě/ř/š/etc. render as tofu in Caprasimo).
- Icon font (Bootstrap Icons `bi bi-*`) replaced with **Lucide** icons (stroke-width 2.75), loaded via `https://unpkg.com/lucide@0.453.0/dist/umd/lucide.min.js` + `lucide.createIcons()`.
- Kept, per explicit user instruction: the toolbar concept (back / quiz-check / reading settings / exercises / listen / bookmark / "More" dropdown) and the article renderer (markdown body, math/fraction chips, callouts, image figures, tables) — these were called out as "really good."
- Renamed "Prověrka" (implies a real, scheduled school test) → **"Ověř si porozumění"** ("Check your understanding") everywhere, and reframed it as a per-post feature (lives at the bottom of every article), not a global exam.
- Calendar (`/calendar`) reframed: the app has no knowledge of the user's real school schedule, so it now only shows user-added reminders and public holidays — no invented class periods ("2. hodina") or school-scheduled tests.
- Chosen app shell: **left rail** (subjects + grade picker always visible) over a top-bar alternative that was also explored and rejected (subject row had to scroll horizontally; ToC had to hide in an accordion).

## Screens / Views

All screens share the shell in `Tobiso App.dc.html`: a 264px fixed left sidebar (nav + subjects + grade picker + dark-mode toggle + profile) and a fluid main column, max-width 1100px, padding `var(--space-8)` (35.2px) on all sides except bottom (80px, to clear nothing — no bottom bar in this shell).

### 1. Domů (Home) — `/`
- Greeting `h1` (clamp 34–46px) + one-line status paragraph.
- 3-stat row: streak days, total articles read, AI credits — each a rounded card (`var(--radius-lg)`), the credits card tinted `--color-accent-200`.
- Two-up: "continue reading" card (progress bar) + "article of the day" card (sage-tinted).
- Subject grid: `repeat(auto-fill, minmax(180px,1fr))`, 9 subject tiles (icon, name, article count).

### 2. Nejnovější (Latest) — `/latest`
- Filter pill row (subject filter, one active state via `.pill-on`).
- List of article rows: title + subject breadcrumb, up to 3 grade-availability chips, right-aligned date. Full row is a click target (`class="lift"` hover).

### 3. Předměty → category (Category browse) — `/categories/{parentId?}`
- Breadcrumb nav.
- Subcategory grid (2-line cards: name + count, chevron-right).
- Flat article list for the selected subcategory.

### 4. Post detail (article) — `/post/{id}` — see `PostBody.dc.html`
This is the centerpiece; kept from the old app per user request, retheme only. Full breakdown:
- Breadcrumb (subject / subcategory / title).
- H1 + grade badge (pill, `--color-accent-200` bg / `--color-accent-800` text) + chevron toggle → dropdown listing all grade versions (6./7./9. ročník), active one highlighted.
- Meta row: word count + read time (Lucide `book-open`), last-edited date, "Zkontrolováno" (reviewed) badge in sage.
- **Toolbar** (pill-shaped container, `--color-surface` bg): Zpět (back) · Ověř si porozumění · Čtení (reading settings) · Cvičení (exercises) · Poslech (listen) — then a spacer, bookmark icon button, and a solid-accent "Více" (more) button.
  - Reading-settings panel (toggled): font family / size / column-width triples, each a 3-pill segmented control.
  - "More" panel (toggled): 2-column grid of secondary AI tools (Kartičky/flashcards, Procvičovací úlohy, Přepsat pro ročník, Co může přijít v testu, Reálné využití, Co kdyby, Zkus to vysvětlit, Porovnat s…, Krok za krokem) then a divider then Vytisknout PDF / Tahák PDF / Pomodoro / Poznámky.
- Intro/lead paragraph: full-width tinted block (`--color-accent-200`), 18px, bolded key terms.
- Article prose (`#article-prose`, max-width 68ch, 17px/1.75): h2/h3, lists, inline person-mention chips (dashed underline + search icon), inline fraction chips (rounded mono background) — these represent the math/fraction rendering from `MarkdownContent.razor`'s `ReplaceFractionsInText`.
- Tip callout (amber/terracotta `#ffe1d0` bg) and Zajímavost/sidefact callout (sage `#e1eecc` bg) — map to the `!!!tip` / `!!!sidefact` markdown syntax already in the pipeline.
- Figure: `.washed`-treated image (desaturated per Organic imagery rules) + caption/source row.
- Data table (Přehled) using themed `<th>`/`<td>` on `--color-surface` header.
- "Věděl jsi, že…?" collapsible fun-facts panel.
- Interactive exercises: 2-up card grid (drag-drop, matching types shown; timeline/circuit follow the same card pattern).
- AI chat-on-article panel: input + send button + 3 suggested-prompt pills + credits badge.
- Related posts: 3-up card grid.
- Difficulty rating bar (Snadné/Střední/Těžké pills, selected state = solid accent).

### 5. Procvičování (Practice hub) — `/practice`
- 3 mode cards: Kartičky (flashcards), **Ověř si porozumění** (redirects into a per-article quiz — the hub just aggregates access), AI zkoušení.
- Below: a browsable question bank by subject (filter pills + question preview rows) — this view is an aggregator; the actual "Ověř si porozumění" quiz UI lives on the article page.

### 6. Quiz runner (question flow) — reachable from Practice or an article's toolbar
- Progress label + progress bar.
- One question per screen (radio-style answer list, correct answer shown solid sage once picked), explanation callout, Předchozí/Další navigation.

### 7. AI chat (standalone) — `/chat`
- 2-column: chat thread (bubbles: user = solid terracotta right-aligned, assistant = neutral card left-aligned with a "source: article X" footer) + input row; right rail lists saved chat sessions.

### 8. Záložky (Bookmarks) — `/zalozky`
- Card grid, each card: subject tag, title, saved date, remove (×) button.

### 9. Kalendář (Calendar) — `/calendar`
- Month grid (7 cols), only days with self-added reminders or public holidays get colored fill + label (terracotta = user reminder, sage = holiday/deadline). See "What Changed" — no invented school periods.
- "Nejbližší" (upcoming) list below with the same event typing.

### 10. Profil (Profile) — `/profil`
- Avatar + name/email/grade header, logout button.
- 4-stat row (streak / read / credits / badges).
- Per-subject progress bars.
- Badge row (earned = solid tint, locked = dashed outline + lock icon).

### 11. Zpětná vazba (Feedback) — `/feedback`
- Category pills, article reference field, message textarea, submit button.

### 12. Přihlášení (Login) — `/prihlaseni`
- Centered card, email/password fields, primary submit, divider, Google OAuth button, register link mentioning the 20-credit signup bonus.

## Interactions & Behavior
- All toggles (version picker, reading panel, "more" panel, facts panel, bookmark) are simple boolean show/hide — no animation specified; a fade/slide of ~150ms would match the system's restraint but wasn't in-scope to add.
- Nav rail item active state = filled pill (`--color-accent-200` bg, bold text).
- Pills (`.pill`) follow the Organic hover pattern: transparent → `--color-neutral-100` on hover; `.pill-on` = solid `--color-accent-600`.
- Cards with class `lift` get a background shift to `--color-surface` on hover (no lift/shadow motion).
- Difficulty rating and quiz answers are local click-state only in the prototype — wire to real submission endpoints per `ARCHITECTURE.md` §7–9.

## State Management (prototype only — map to real data in the app)
- Current screen (routing) — in the prototype this is a single `state.screen` string switched by nav clicks; in the real app this is just Blazor routing (`@page`) as already structured.
- Per-post: `versionsOpen`, `moreOpen`, `readingOpen`, `factsOpen`, `bookmarked`, `rating` — these map directly to the existing `PostDetail.razor` state fields already documented in `ARCHITECTURE.md` §5 (`_showVersionPicker`, `_allVersions`, etc.) — no new state shape needed, just restyle.

## Design Tokens
All from the **Organic** design system (`_ds/organic-.../styles.css`) — do not hardcode; port these into `Tobiso.Web.App/wwwroot/css/variables.css`:

**Color**
- `--color-bg: #f5ead8` (page ground)
- `--color-surface: #ebddc5` (raised panels/toolbar)
- `--color-text: #201e1d`
- `--color-accent: #c67139` (terracotta) — ramp 100–900, e.g. `--color-accent-600: #b2622d` (buttons/links), `--color-accent-200: #ffe1d0`-ish tints for badges
- `--color-accent-2: #7a8a5e` (sage, second voice) — ramp 100–900
- `--color-neutral-100…900` — the neutral ramp used for hover states and dividers
- `--color-divider: color-mix(in srgb, #201e1d 16%, transparent)`

**Type**
- `--font-heading`: system default is Caprasimo — **this project overrides it to `"Baloo 2", system-ui, sans-serif`** (loaded via Google Fonts `Baloo+2:wght@400;600;700`) specifically because Caprasimo does not render Czech diacritics (ě, ř, š, ž, ý, etc.). Keep this override; do not revert to Caprasimo for Czech copy.
- `--font-body`: Figtree

**Spacing / radius / shadow**
- `--space-1…8`: 4.4px–35.2px (1.10× density scale)
- `--radius-sm/md/lg`: 8/16/28px; pills use `border-radius:999px`
- `--shadow-sm/md/lg` for elevation (used sparingly — modal-style panels only)

**Icons**: Lucide, stroke-width 2.75, loaded via `<script src="https://unpkg.com/lucide@0.453.0/dist/umd/lucide.min.js">` then `lucide.createIcons({attrs:{'stroke-width':2.75}})` on mount/update.

## Assets
No custom images — the article figure uses a placeholder drop-slot (`<image-slot>`, a prototyping-only component). In the real app this is just `<img>` sourced from `files.tobiso.com` as today. No other custom icons or illustrations were introduced; all iconography is stock Lucide glyphs referenced by name in the HTML (e.g. `data-lucide="bookmark"`).

## Files
- `Tobiso App.dc.html` — the full app shell with all 12 screens wired via a simple screen-switch (open in a browser to click through).
- `Tobiso.dc.html` — the earlier two-shell comparison (left-rail vs. top-bar); left-rail (labelled "1a") is the one chosen and carried into `Tobiso App.dc.html`.
- `PostBody.dc.html` — the standalone article/post renderer component, reused via import inside the app shell's post screen.
- `Exercise Types.dc.html` — later addendum covering `drag-drop`/`matching`/`timeline` interactive exercise UI on the same Organic tokens; **supersedes** this doc's exercise interactions (see Implementation Status above).
- `ARCHITECTURE.md` (repo root) — the source of truth for data model, routing, and existing feature scope this redesign maps onto.
- `sync-log.md` — sync record (originally `github.md`) noting which repo files this design was built from.
