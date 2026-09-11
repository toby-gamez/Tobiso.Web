# Tobiso.Web — Full Architecture & Feature Reference

## Table of Contents

1. [Solution Structure](#1-solution-structure)
2. [Domain Model](#2-domain-model)
3. [Grade System & PostVersion](#3-grade-system--postversion)
4. [Post Rendering Pipeline](#4-post-rendering-pipeline)
5. [Post Detail Page](#5-post-detail-page)
6. [AI Features](#6-ai-features)
7. [Interactive Exercises](#7-interactive-exercises)
8. [Student Accounts](#8-student-accounts)
9. [Search](#9-search)
10. [PDF Export](#10-pdf-export)
11. [Push Notifications](#11-push-notifications)
12. [Authentication](#12-authentication)
13. [Navigation & Layout](#13-navigation--layout)
14. [Admin Panel](#14-admin-panel)
15. [CSS Architecture](#15-css-architecture)
16. [App Bootstrap & DI](#16-app-bootstrap--di)
17. [Routing Reference](#17-routing-reference)

---

## 1. Solution Structure

Six projects — two executable hosts, three libraries, one static file server.

| Project | Type | Role |
|---------|------|------|
| `Tobiso.Web.App` | ASP.NET Core host | Public frontend (Blazor Server) + REST API for students |
| `Tobiso.Web.App.Admin` | ASP.NET Core host | Admin dashboard (Blazor Server) + admin API |
| `Tobiso.Web.Api` | Class library | All business-logic services + EF Core data access |
| `Tobiso.Web.Domain` | Class library | EF Core entity classes only |
| `Tobiso.Web.Shared` | Class library | DTOs + Refit interface definitions |
| `Tobiso.Web.Files` | Static file host | Image/asset CDN at `https://files.tobiso.com` |

Both hosts reference `Tobiso.Web.Api` directly and call the database in-process. They **also** register Refit clients (`ITobisoAnonymApi`, `ITobisoWebApi`) that HTTP-call `Tobiso.Web.App`'s own API — this is used by Blazor pages that need to call through the REST layer (e.g. for auth).

Running:
```bash
dotnet run --project Tobiso.Web.App        # https://localhost:7270
dotnet run --project Tobiso.Web.App.Admin  # https://localhost:7271
```

---

## 2. Domain Model

All entities live in `Tobiso.Web.Domain/Entities/`.

### Core Content Entities

**`Post`** — A topic/article.
- `Id`, `Title`, `FilePath` (slug-like, used for URL routing), `CreatedAt`, `CategoryId`
- Navigation: `ICollection<PostVersion> Versions`, `ICollection<Question> Questions`, `ICollection<InteractiveExercisePost> InteractiveExercisePosts`
- Content is NOT stored on `Post` — it lives entirely in `PostVersion`.

**`PostVersion`** — One grade-specific version of a post's content.
- `Id`, `PostId`, `GradeId`
- `string Content` — full Markdown body
- `DateTime? LastFix` — minor correction timestamp
- `DateTime? LastEdit` — major edit timestamp
- Unique constraint: `(PostId, GradeId)` — one version per grade per post.

**`Grade`** — A school year level.
- `Id`, `Name` (e.g. "6. ročník"), `Level` (integer: 6, 7, 8, 9)
- Seeded on startup by `GradeService.SeedDefaultsAsync()`.
- Deletion blocked if any `PostVersion` references the grade.

**`Category`** — Hierarchical subject taxonomy.
- `Id`, `Name`, `Slug`, `ParentId` (self-referencing), `List<Category> Children`
- Computed (not mapped): `FullPath` = `"Parent > Child"` chain

**`RelatedPost`** — Links two posts together.
- `Id`, `PostId`, `RelatedPostId`, `Text?` (optional explanatory note)
- Unique constraint prevents self-references.

**`Addendum`** — Supplementary note embedded inline in post content.
- Referenced by the `(--DOD-{id}--)` marker syntax in post Markdown.

### Student/User Entities

**`AppUser`** — Student account.
- `Id`, `Email`, `DisplayName`, `PasswordHash` (PBKDF2), `GoogleId?`, `AvatarUrl?`, `Credits` (int), `Role` ("student")
- Navigation: `ICollection<AiCreditTransaction>`, `ICollection<UserBookmark>`, `ICollection<UserReadPost>`

**`AiCreditTransaction`** — Credit history log.
- `Id`, `UserId`, `Delta` (positive = credit added, negative = deducted), `Reason` (string), `CreatedAt`

**`UserBookmark`** — Saved post.
- `UserId`, `PostId`, `CreatedAt`

**`UserReadPost`** — Reading progress.
- `UserId`, `PostId`, `FirstReadAt`, `LastReadAt`, `MaxScrollPercent`

### AI Cache Entities

These tables cache AI-generated content per post to avoid redundant API calls:
- `PostFunFacts` — fun facts, keyed by `PostId` + `GeneratedAt >= postLastEdit`
- `PostKeyTerms` — key term definitions
- `PostConceptMaps` — concept map JSON
- `PostCrossConnections` — cross-topic connections
- `PostAiDemos` — interactive demo JSON

### AI Chat Entities

**`AiChatSession`** — One session per `(UserId, PostId)` pair.

**`AiChatMessage`** — Individual message.
- `Id`, `SessionId`, `Role` ("user"/"assistant"), `Content`, `CreditsUsed`, `CreatedAt`

### Other Entities

- `InteractiveExercise` — Exercise config (`ConfigJson`, `SolutionJson`, `Type`)
- `InteractiveExercisePost` — Links exercise to post
- `InteractiveExerciseCategory` — Links exercise to category (applies to all posts in that category)
- `Question` / `Answer` — Multiple-choice quiz questions per post
- `Event` — Calendar event (title, description, date)
- `Feedback` — Student feedback submission
- `DeviceToken` — FCM token for push notifications

---

## 3. Grade System & PostVersion

### The Core Idea

A single `Post` (topic) can have multiple `PostVersion` records — one per school year (`Grade`). This lets the same article exist in an age-appropriate version for 6th, 7th, 8th, or 9th graders. Grades currently seeded: levels 6, 7, 8, 9.

### Best-Match Algorithm

`PostService.BestMatch()` selects the right version when a preferred grade level is given:

```csharp
private static PostVersion? BestMatch(IEnumerable<PostVersion> versions, int preferredLevel)
{
    // Take the highest grade ≤ preferred; fall back to highest available
    return versions.Where(v => v.Grade.Level <= preferredLevel)
                   .OrderByDescending(v => v.Grade!.Level).FirstOrDefault()
        ?? versions.OrderByDescending(v => v.Grade?.Level).FirstOrDefault();
}
```

A 7th-grader will get the 7th-grade version if it exists, or 6th-grade if not. Never sees content above their level.

### How Grade Preference Is Stored

`PreferenceService` wraps Blazor's `ProtectedLocalStorage`:
- Key: `"preferredGradeId"`
- Set from the grade picker in the nav sidebar (dropdown shows all grades from API)
- Read on every PostDetail page load

### Grade Switching on PostDetail

```
User visits /post/{id}
  → PreferenceService.GetPreferredGradeIdAsync() → reads browser storage
  → Api.GetPostById(id, gradeId) → returns PostResponse with single best-match version
  → LoadAllVersions(id) → Api.GetPostById(id, no gradeId) → _allVersions = all versions

Display:
  - ActiveVersion = _selectedVersionGradeId ?? _allVersions.First()
  - Grade badge shows "6. ročník" / "7. ročník" etc.
  - If _allVersions.Count > 1 → chevron button → version picker dropdown

User selects grade:
  → SelectVersion(gradeId) → _selectedVersionGradeId = gradeId
  → ActiveVersion recomputes from _allVersions (already loaded, no re-fetch)
  → MarkdownContent re-renders with new Content
  → ExtractArticleImages() updates the image gallery
```

Key: switching grades is **client-side only** after the first load — all versions are prefetched into `_allVersions`.

### Grade Display UI — Exact Razor Structure

The grade badge and picker live in the post title row of `PostDetail.razor`:

```razor
<!-- Grade badge — always visible -->
<span class="grade-badge">@ActiveVersion?.GradeName</span>

<!-- Chevron toggle — only shown when multiple versions exist -->
@if (_allVersions?.Count > 1)
{
    <button class="version-picker-toggle" @onclick="ToggleVersionPicker">
        <i class="bi bi-chevron-down"></i>
    </button>
}

<!-- Dropdown — shown/hidden by _showVersionPicker -->
@if (_showVersionPicker)
{
    <div class="version-picker-dropdown">
        @foreach (var v in _allVersions!)
        {
            <button class="@(v.GradeId == ActiveVersion?.GradeId ? "active" : "")"
                    @onclick="() => SelectVersion(v.GradeId)">
                @v.GradeName
            </button>
        }
    </div>
}
```

**State fields:**
```csharp
private int? _selectedVersionGradeId = null;       // null = use server-matched version
private List<PostVersionResponse>? _allVersions;   // all grade versions, fetched once
private bool _showVersionPicker = false;

private PostVersionResponse? ActiveVersion =>
    _selectedVersionGradeId.HasValue
        ? (_allVersions?.FirstOrDefault(v => v.GradeId == _selectedVersionGradeId)
           ?? _post?.Versions?.FirstOrDefault())
        : _post?.Versions?.FirstOrDefault();

private void ToggleVersionPicker() => _showVersionPicker = !_showVersionPicker;

private void SelectVersion(int gradeId)
{
    _selectedVersionGradeId = gradeId;
    _showVersionPicker = false;
    ExtractArticleImages();  // re-extract image gallery for new version content
}
```

**What `ActiveVersion` actually drives:**
- `<MarkdownContent Content="@ActiveVersion?.Content" />` — main article body
- `@ActiveVersion?.GradeName` in the grade badge
- Table of contents extraction (JS `extractHeadings` is called after re-render)
- Image gallery (re-built by `ExtractArticleImages()`)
- `LastEdit` / `LastFix` timestamps shown in the footer of the article

**The two API calls on load:**

| Call | Parameters | Purpose |
|------|-----------|---------|
| `Api.GetPostById(id, gradeId)` | with preferred grade | Fast initial render — single best-match version in `_post.Versions[0]` |
| `Api.GetPostById(id)` | no grade | Loads all versions → `_allVersions` (enables grade picker) |

Both calls happen on `OnInitializedAsync`. The second call populates `_allVersions` which makes the picker appear. If a post has only one version, `_allVersions.Count == 1`, the chevron is hidden and the badge is non-interactive.

### AI Rewrite vs. Grade Switching

These are two separate and independent mechanisms:

| Feature | What it changes | Stored? | Source |
|---------|----------------|---------|--------|
| Grade picker | Switches between authored `PostVersion` content | No (session-only) | DB via API |
| AI Rewrite | Replaces content with AI-generated paraphrase | No (session-only) | OpenAI |

The AI rewrite temporarily sets `_rewrittenContent` in PostDetail. When active, `<MarkdownContent Content="@_rewrittenContent" />` is used instead of `ActiveVersion.Content`. The grade picker still operates on the original versioned content; switching grades while a rewrite is active clears the rewrite.

`GradeRewriter.razor` is a modal component where the user picks a target level (4–9) or a register ("simple"/"student"/"expert"). It calls:
- `POST /api/ai/rewrite-grade` — rewrites for a school year level
- `POST /api/ai/rewrite-register` — rewrites in a language register

The result is rendered using `<MarkdownContent>` inside the modal, and the user can "apply" it to replace the main content view temporarily.

### API DTOs

**`PostResponse`**: `Id`, `Title`, `FilePath`, `CategoryId`, `List<PostVersionResponse> Versions`
- With `gradeId` param → `Versions` has exactly 1 (best-match) element
- Without `gradeId` → `Versions` has all versions (used by PostDetail's `LoadAllVersions` call and admin)

**`PostVersionResponse`**: `Id`, `PostId`, `GradeId`, `GradeName`, `GradeLevel`, `Content`, `LastFix`, `LastEdit`

**`PostSummaryResponse`** (list view): `Id`, `Title`, `CategoryId`, `FilePath`, `LastEdit`, `LastFix`, `List<string> AvailableGradeNames`

---

## 4. Post Rendering Pipeline

The entire pipeline runs inside `MarkdownContent.razor` via `TransformMarkdownContent(content)`. The steps run **in order**:

### Step 1 — Pre-processing (before Markdig)

1. **mailto normalization** — `ReplaceMarkdownMailtoInText`: normalizes `mailto:` link syntax.
2. **Fraction substitution** — `ReplaceFractionsInText`: converts `1/2`, `{n+1}/{n-1}` etc. to `<span class='math-inline' data-math="\frac{1}{2}">` KaTeX spans. Skips content inside code fences.
3. **Strikethrough** — `ReplaceStrikethroughSyntax`: `#s#text#s#` → `<del>text</del>`
4. **Person mentions** — `ReplacePeopleMentionsInText`: wraps detected person names (pre-detected by the `/api/ai/detect-persons` endpoint) in `<span class="person-mention person-link" data-person-name="...">` with a search icon click handler.
5. **Arrow substitution** — ` -> ` → Bootstrap icon arrow span.
6. **Image group preprocessing** — `PreprocessImageGroups`: groups consecutive `![alt](src)` image lines with optional caption/source paragraphs into `<div class="blog-image">` wrappers before Markdig sees them.

### Step 2 — Markdig

```csharp
Markdig.Markdown.ToHtml(content)  // standard pipeline, default extensions
```

### Step 3 — Post-processing (after Markdig)

7. **Addendum markers** — `(--DOD-123--)` → inline `<a onclick="window.requestAddendum(123)">` anchor.
8. **Image src fixing** — relative `/images/...` paths → `https://files.tobiso.com/images/...`
9. **Link processing**:
   - External links → `target="_blank"` added
   - Internal links resolved against `AllPosts` by `FilePath` → `/post/{slug}`
   - Unresolved links → struck-through gray text (no dead links navigating anywhere)
10. **Intro wrapper** — `...text...` → `<div class="intro">text</div>`
11. **Callout blocks**:
    - `!!!tip content !!!` → `<div class="callout-block callout-tip">`
    - `!!!sidefact content !!!` → `<div class="callout-block callout-sidefact">`
12. **Code block unescaping** — fixes math placeholder escaping inside `<code>` tags.
13. **Table fallback** — `ConvertMarkdownTablesToHtml`: fallback parser for pipe-tables Markdig misses.
14. **Table scroll wrapping** — `WrapTablesForScroll`: wraps every `<table>` in `<div class="table-scroll-wrapper">` for mobile horizontal scroll.

### Math Rendering

KaTeX `0.16.8` is loaded dynamically from CDN in `OnAfterRenderAsync`. Delimiters:
- `$...$` → inline math
- `$$...$$` → display math
- `[data-math]` spans → fraction pre-processing output

### AI Content Rendering

For AI-generated content (flashcards, rewrite results, chat answers), `AiMarkdown.cs` uses a stricter Markdig pipeline:

```csharp
MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .DisableHtml()   // escapes raw HTML — prevents XSS from prompt injection
    .Build()
```

---

## 5. Post Detail Page

`Tobiso.Web.App/Components/Pages/PostDetail.razor` — routes: `/post/{Id:int}` and `/post/{Slug}`.

This is the largest component in the app.

### Data Loading

On init:
1. Resolve post by ID or slug.
2. Get preferred grade from `PreferenceService`.
3. `Api.GetPostById(id, gradeId)` → single-version `PostResponse`.
4. `LoadAllVersions(id)` → all versions, stored in `_allVersions`.
5. Load related posts, addendums, fun facts (from cache or AI).
6. Load bookmarks, reading progress.
7. JS: `extractHeadings()` → build table of contents.

### UI Sections (top to bottom)

- **Breadcrumb** — category ancestors as clickable links
- **Title + grade badge** — grade name shown; chevron opens version picker if multiple versions exist
- **Tools toolbar (top)** — back button, quiz mode, focus mode, reading settings, share, PDF, TTS, "More" dropdown
- **Table of contents** — wiki-style, extracted from H2/H3 headings via JS
- **Main content** — `<MarkdownContent>` for `.md` files; raw `MarkupString` for HTML
- **AI rewrite panel** — inline replacement; user picks grade level 4–9 or register (simple/student/expert); calls `/api/ai/rewrite-grade` or `/api/ai/rewrite-register`
- **Fun facts** — collapsible; loaded from `PostFunFacts` cache or generated on demand
- **Notes** — user text area, stored in localStorage
- **Interactive exercises** — collapsible section, loads `DragDropExercise`, `MatchingExercise`, `TimelineExercise`, or `CircuitSimulator`
- **Video float button** — opens embedded video if linked
- **AI context cards (BLOK 4)**:
  - `<AiInteractiveDemo>` — AI-generated interactive activity
  - `<ConceptMapCard>` — visual concept map
  - `<CrossConnectionsCard>` — cross-topic connections
- **AI chat box** — inline chat anchored to this post's context
- **Related posts section**
- **Tools toolbar (bottom)** — mirrors top toolbar
- **Difficulty rating widget** — appears after scrolling; Easy/Medium/Hard; stored in localStorage

### Modal Components (all lazy/conditional)

| Modal | Trigger |
|-------|---------|
| `AddendumModal` | Click on `(--DOD-xxx--)` inline markers |
| `PersonModal` | Click on person-mention spans |
| `FlashcardDeck` | Toolbar "More" → Flashcards |
| `PracticeProblems` | Toolbar "More" → Practice |
| `FeynmanMode` | Toolbar "More" → Feynman |
| `RealWorldModal` | Toolbar "More" → Real world |
| `WhatIfModal` | Toolbar "More" → What if |
| `ExamPredictorModal` | Toolbar "More" → Exam predictor |
| `CompareModal` | Toolbar "More" → Compare |
| `StepSolverModal` | Toolbar "More" → Step solver |
| `StudyTimer` | Toolbar "More" → Timer |
| `CheatSheetModal` | Toolbar PDF → Cheat sheet (4 ratio options) |
| `GradeRewriter` | Toolbar rewrite → AI grade picker |

### Reading Preferences

Persisted in `localStorage`:
- Font family (sans / serif / mono)
- Font size (small / medium / large)
- Line width (narrow / medium / wide)

Controlled by the reading-settings panel in the toolbar.

---

## 6. AI Features

### Main Chat Endpoint

`POST /api/ai/ask` (and `POST /api/ai/ask-stream` for SSE)

Request: `AiChatRequest { PostId, Question, ConversationHistory[], SocraticMode }`

Flow:
1. Resolve rate limit key: `ip:<RemoteIpAddress>` (server-side, not spoofable)
2. Check `AiRateLimitService.TryConsume(key, limit)` → `HTTP 429` if exceeded
3. Call `IAiService.AskAsync()` → hits OpenAI-compatible external API
4. If student is authenticated: deduct 1 credit, save messages to `AiChatSession`/`AiChatMessage`
5. Return `AiChatResponse { Answer, RemainingQuestions }`

### Rate Limiting

`AiRateLimitService` is a singleton `ConcurrentDictionary` keyed by IP.

- Default daily limit from `OpenAI:MaxDailyRequests` config (default: 10)
- Per-client overrides: `OpenAI:ClientLimits:<clientId>` config keys; selected by `X-Client-Id` header
- Bonus credits added via `POST /api/ai/credits` — verified with HMAC-SHA256 (`OpenAI:CreditsSigningSecret`)
- Anonymous users: IP-based counter. Students: credits from DB (`AppUser.Credits`), if 0 → `HTTP 402`

### AI Cache Layer

Several endpoints cache results in the database to avoid redundant calls. Cache hit condition: `GeneratedAt >= postLastEdit`. Cached endpoints:

| Endpoint | Cache Table | Key |
|----------|-------------|-----|
| `POST /api/ai/fun-facts` | `PostFunFacts` | `PostId` |
| `POST /api/ai/key-terms` | `PostKeyTerms` | `PostId` |
| `POST /api/ai/concept-map` | `PostConceptMaps` | `PostId` |
| `POST /api/ai/cross-connections` | `PostCrossConnections` | `PostId` |
| `POST /api/ai/generate-demo` | `PostAiDemos` | `PostId` |

Force-regeneration is triggered by passing `?force=true` (adds a cache-busting prompt-version timestamp).

### Full AI Endpoint List

All 20+ endpoints on `AiController`, all rate-limited:

| Endpoint | Purpose |
|----------|---------|
| `ask` / `ask-stream` | Main AI chat (SSE streaming variant) |
| `explain-sentence` | Explain a selected sentence |
| `evaluate-answer` | Check a student's free-text answer |
| `flashcards` | Generate flashcard deck |
| `practice-problems` | Generate practice questions |
| `rewrite-grade` | Rewrite content for a different grade |
| `rewrite-register` | Rewrite in simple/student/expert register |
| `real-world` | Real-world application examples |
| `fun-facts` | Fun facts (cached) |
| `exam-questions` | Predicted exam questions |
| `evaluate-comprehension` | Comprehension quiz |
| `why` | "Why does this matter?" explanations |
| `key-terms` | Key term glossary (cached) |
| `compare` | Compare two concepts |
| `step-solver` | Step-by-step problem solver |
| `generate-demo` | Interactive activity demo (cached) |
| `concept-map` | Concept map JSON (cached) |
| `formula-vars` | Explain variables in a formula |
| `cross-connections` | Cross-topic connections (cached) |
| `detect-persons` | Detect person names in content |
| `grammar-check` | Grammar check (admin only) |
| `generate-question` | Generate a quiz question |

### Chat History

**Server-side** (authenticated students only):
- `GET /api/ai/history` → list sessions
- `GET /api/ai/history/{sessionId}` → messages for a session
- Pattern: one `AiChatSession` per `(UserId, PostId)` — GetOrCreate

**Client-side localStorage** (all users):
- `RightSidebar.razor` reads `tobiso_ai_chats` from localStorage
- Stores `{ Id, PostId, PostTitle, FirstQuestion, UpdatedAt }` entries
- Shown in the "Chaty" tab of the right sidebar

---

## 7. Interactive Exercises

### 5 Exercise Types

Defined in `Tobiso.Web.Shared/DTOs/ExerciseTypeConstants.cs`:

| Type constant | Value | Blazor component |
|--------------|-------|-----------------|
| `DragDrop` | `"drag-drop"` | `DragDropExercise.razor` |
| `Matching` | `"matching"` | `MatchingExercise.razor` |
| `Timeline` | `"timeline"` | `TimelineExercise.razor` |
| `Circuit` | `"circuit"` | `CircuitSimulator.razor` |
| `Molecule` | `"molecule"` | (in progress) |

### Config JSON Shapes

**Drag-Drop:**
```json
{
  "categories": [{ "id": "cat-1", "label": "Příroda" }],
  "items": [{ "id": "item-1", "text": "Dub" }]
}
```
Solution: `{ "correctPlacements": { "item-1": "cat-1" }, "explanation": "..." }`

**Matching:**
```json
{
  "left": [{ "id": "l-1", "text": "H₂O" }],
  "right": [{ "id": "r-1", "text": "Voda" }]
}
```
Solution: `{ "pairs": [{ "id": "pair-1", "leftId": "l-1", "rightId": "r-1" }], "explanation": "..." }`

**Timeline:**
```json
{
  "timeRange": { "start": 1300, "end": 1700 },
  "events": [{ "id": "e-1", "label": "Objev Ameriky", "year": 1492 }]
}
```
Solution: `{ "correctOrder": ["e-1", "e-2"], "explanation": "..." }`

**Circuit:** Open sandbox — user adds components (battery, bulb, switch, resistor, capacitor, LED, motor, buzzer) and clicks to connect. `CircuitSimulator.razor` runs Ohm's Law in C# to compute voltage/current/power and detects closed circuits via DFS graph traversal. No fixed `ConfigJson` — validation falls back to JSON equality.

### Validation

`InteractiveExerciseService.ValidateSolutionAsync()` — server-side, returns `ExerciseValidationResult { IsCorrect, Score (0–100), Feedback, Explanation, DetailedResults }`.

Scoring:
- **Timeline** — binary (full correct order or not)
- **Drag-Drop** — partial: `score = correct_placements / total_items × 100`
- **Matching** — partial: `score = correct_pairs / total_pairs × 100`
- **Circuit** — JSON equality fallback

### Exercise Discovery

Exercises are discovered for a post through two paths:
1. **Direct link** — `InteractiveExercisePosts` join table (exercise linked to specific post)
2. **Category inheritance** — `InteractiveExerciseCategories`: exercise linked to a category applies to all posts in that category and any descendant categories (walks ancestor tree in-memory)

---

## 8. Student Accounts

### Registration & Login

**Email/password** (`/registrace`):
1. `POST /api/auth/register` with `RegisterRequest(Email, DisplayName, Password)` (min 8 chars, validated client-side)
2. `UserService` creates `AppUser`, hashes password with ASP.NET `PasswordHasher<AppUser>`
3. Grants **20 registration bonus credits** via `AiCreditTransaction`
4. Returns `StudentLoginResponse { Token, DisplayName, Credits }`
5. Token stored to `localStorage["tobiso_student_token"]`, auth state notified

**Google OAuth** (`/prihlaseni` → Google button):
1. `GET /api/auth/google-login` → ASP.NET `Challenge("Google")`
2. Callback at `/api/auth/google-callback` → `UserService.FindOrCreateGoogleUserAsync()` (links to existing email or creates new user with 20 credits)
3. Issues student JWT, redirects to `/google-auth-complete?token=...`
4. `GoogleAuthComplete.razor` reads token from query string → stores in `StudentCredentialStore`

**Admin** (`/login` in admin app):
1. `POST /api/auth/login` with `{ username, password }` (against `Auth:Basic` config values)
2. Returns `{ token }` (admin JWT, 24-hour default)
3. Stored to `localStorage["tobiso_jwt"]`

### Credits System

| Event | Delta |
|-------|-------|
| Registration | +20 |
| Google registration | +20 |
| AI `ask` call | −1 |
| Bonus via `/api/ai/credits` (HMAC-verified) | +N |

All changes logged in `AiCreditTransactions` with `Delta` and `Reason`. If credits = 0 on `ask` → `HTTP 402` "Nemáš dostatek kreditů." Anonymous users bypass credits and use IP-based daily limit instead.

Student JWT claims: `email`, `role=student`, `credits`, optional `avatar`.

### Bookmarks

- Add: `POST /api/student/bookmarks/{postId}` → inserts `UserBookmark`
- Remove: `DELETE /api/student/bookmarks/{postId}`
- List: `GET /api/student/bookmarks` → returns bookmark IDs
- UI: shown on `/zalozky` page; bookmark icon on PostDetail toolbar; stored both in DB (authenticated) and localStorage (fallback)

### Reading Progress & Streak

`UserProgressService.UpsertReadProgressAsync(userId, postId, scrollPercent)` — upserts `UserReadPosts` with `FirstReadAt`, `LastReadAt`, `MaxScrollPercent`.

**Streak calculation:** counts consecutive distinct UTC calendar days backwards from today through `LastReadAt` timestamps. Lenient: allows yesterday as streak start even if today hasn't been read yet.

### Badges

Earned automatically from `UserReadPosts`, grouped by root category:

| Condition | Badge |
|-----------|-------|
| 5+ articles in a subject | "Začátečník" |
| 15+ articles in a subject | "Pokročilý" |
| 30+ articles in a subject | "Expert" |

Returned in `UserStatsDto { StreakDays, TotalRead, PerSubject[], Badges[] }`.

### Profile Page (`/profil`)

Shows: display name, email, credits balance, `CreditBadge` widget, streak days, total articles read, per-subject counts, earned badges.

---

## 9. Search

Entirely client-side JavaScript in `wwwroot/js/blazor-utils.js`.

### Index Loading

On page load, `loadSearchIndex()` fetches in parallel:
- `GET /api/Pages` — all posts with content (full versions)
- `GET /api/Pages/categories` — all categories

Maps posts to `{ url, title, content, categoryName, categoryFullPath, topCategoryName }` and categories to `{ url, title, fullPath, pathAbove }`.

### Search Algorithm

Triggered at ≥ 2 characters typed (debounced).

Scoring:
| Match | Score |
|-------|-------|
| Title contains query | +10 |
| Category name contains query | +7 |
| Content contains query | +5 |

Categories always score 20 and appear first (up to 3). Up to 5 post results shown (8 total max). Text is normalized via `normalizeText()` (lowercase, strip diacritics) for accent-insensitive Czech search.

Results are sorted descending by score. No fuzzy matching — pure substring inclusion.

### UI

`SearchModal.razor` is an HTML shell with no C# code-behind. All behavior (keyboard navigation, result rendering, navigation) is in `blazor-utils.js`. Keyboard shortcut: `K` opens the search modal. Arrow keys navigate results; Enter navigates; Esc closes.

---

## 10. PDF Export

Two parallel PDF systems:

### Server-side PDF (QuestPDF)

`PdfService.cs` at `Tobiso.Web.Api/Services/PdfService.cs`

**`GeneratePdf(PdfRequestDto { Html, FileName })`:**
- Parses HTML with HtmlAgilityPack
- Renders A4 PDF via QuestPDF
- Supports: H2–H6 headings, paragraphs, ul/ol lists (nested), images (downloaded from tobiso.com), tables, bold/italic
- KaTeX math: detects `<span data-math="...">` and renders fractions as stacked `num/den` visual layout

**`GenerateCheatSheetPdf(title, bulletText, ratio)`:**
- Compact mini-PDF (10 cm or 18 cm wide) for cheat sheets
- 4 ratio options: 1×1, 2×1, 1×2, 2×2 (column layout variants)
- Parses `##`/`###` as section headers, `-`/`•` as bullets
- Sized to fit content exactly (not fixed page count)
- Uses QuestPDF Community license

### Client-side PDF (html2pdf.js)

`PdfButton.razor` + `PdfJsInterop` service

- Lazily loads `html2pdf.js 0.9.3` from CDN
- Clones `#content` element, strips buttons/nav/ribbons from clone
- Renders portrait A4 at 2× scale with JPEG quality 0.95
- Saves directly to browser download

---

## 11. Push Notifications

`PushNotificationService.cs` at `Tobiso.Web.Api/Services/PushNotificationService.cs`

Uses **Firebase Admin SDK** (`FirebaseAdmin.Messaging`).

**When triggered:** When a student submits feedback (`/feedback` page → `POST /api/feedback`).

**Flow:**
1. Check `FirebaseApp.DefaultInstance != null` (configured via `Auth:Firebase:CredentialsPath`)
2. Fetch all `DeviceToken` rows from DB
3. Build one `Message` per token: `{ data: { feedbackId, feedbackType, platform, title } }`
4. `FirebaseMessaging.DefaultInstance.SendEachAsync(messages)` batch send
5. Auto-cleanup: tokens that return `Unregistered` or `SenderIdMismatch` error codes are deleted

Device tokens are registered by the Android app via `POST /api/devices/register` → `DeviceService`.

Credentials file: `Tobiso.Web.App/tobisofeedback-firebase-adminsdk-fbsvc-3b9e05c870.json` (gitignored).

---

## 12. Authentication

### SmartAuth Policy Scheme

Configured in both `Program.cs` files. Dispatches based on `Authorization` header prefix:
- `Bearer ...` → `ManualJwtAuthHandler` (validates HS256 JWT: signature, issuer, audience, expiry)
- Anything else → `BasicAuthHandler` (verifies against PBKDF2 `PasswordHash` from DB)

### Student Auth State (Blazor)

`StudentAuthStateProvider` extends `AuthenticationStateProvider`. Reads from `StudentCredentialStore`:
- Singleton store using `AsyncLocal<string?>` (per Blazor circuit) + static `_directToken` fallback
- Token persisted to `localStorage["tobiso_student_token"]`
- `UserMenu.razor` calls `StudentCredentialStore.InitializeAsync()` on first render to restore from localStorage

### Admin Auth State (Blazor Admin App)

`TokenAuthenticationStateProvider` — parses JWT claims client-side from `localStorage["tobiso_jwt"]`.  
`AuthenticationHeaderHandler` — injects `Bearer {token}` into outgoing Refit API calls.

### JWT Token Types

`JwtTokenService` issues two variants:

**Admin token** (24h default):
- `GenerateToken(username, password)` validates against `Auth:Basic:Username`/`Password` config

**Student token** (30 days):
- `GenerateStudentToken(AppUser user)` includes claims: `email`, `role=student`, `credits`, optional `avatar`

### TempCookie

A 10-minute cookie scheme used only for Google OAuth callback handoff — registered separately, not part of SmartAuth.

---

## 13. Navigation & Layout

### Public App Structure

```
MainLayout.razor
  ├── .sidebar → NavMenu.razor (fixed, 285px expanded / 60px collapsed)
  ├── <main> → <article class="content px-4"> → @Body
  ├── UserMenu.razor (fixed top-right)
  ├── RightSidebar.razor (collapsible right panel)
  ├── SearchModal.razor (overlaid)
  ├── PostsGraphModal.razor (overlaid)
  ├── Ribbons.razor
  ├── Cookie consent banner
  └── AppInstallBanner.razor
```

**NavMenu.razor** — Fixed left sidebar with:
- Expand/collapse toggle (state in `localStorage["sidebar-expanded"]`)
- Subject links (Mluvnice, Literatura, Sloh, Hudební výchova, Matematika, Chemie, Fyzika, Přírodopis, Zeměpis) mapped to category IDs
- Zen-grid: Domů, Nejnovější, Síť článků (graph modal), AI Chat, Hledat, Náhodný článek
- Misc: Android app, O mně, Cíl projektu, Zpětná vazba, Deník změn, Kalendář
- Dark mode toggle (class added to `body`)
- Grade picker (ročník) — calls `AnonymApi.GetGrades()`, stores to `PreferenceService`
- Mobile: separate slide-in drawer variant

**UserMenu.razor** — Fixed top-right:
- Authorized: avatar or person icon → dropdown (name, email, credits badge, `/profil` link, logout)
- Not authorized: person icon → navigates to `/prihlaseni`

**RightSidebar.razor** — Collapsible right panel, two tabs:
- "Učivo" — category tree with search filter (loads full category tree from API)
- "Chaty" — AI chat history list (reads `localStorage["tobiso_ai_chats"]`)

**PostsGraphModal.razor** — Full-screen modal showing a force-directed graph of all posts and their relationships, navigable.

**EmptyLayout.razor** — No nav, no sidebar. Used by `/prihlaseni` and `/registrace`.

### Admin App Structure

```
MainLayout.razor
  ├── sidebar → NavMenu.razor (Bootstrap sidebar)
  └── main → top bar (logout button) + @Body
```

Nav links: Home, Categories, Události, Dodatky, Související posty, Zpětná vazba, Cvičení, Images, Grades, Statistiky.

**401-redirect middleware** (Admin only): any 401 not targeting `/login` or `/_` → redirects to `/login`.

---

## 14. Admin Panel

All pages in `Tobiso.Web.App.Admin/Components/Pages/`.

### Post Management (`/`, `/posts/add`, `/posts/edit/{Id:int}`)

- Home (`/`): post list with search, multi-column sort (Updated/Title/Category/ID/FilePath), sort direction toggle, delete (JS confirm), PDF cheat sheet (4 ratio options), navigate to exercises
- Add post: title, file path, category (searchable dropdown), grade version assignment
- Edit post: `MarkdownEditor` component (left: raw Markdown, right: live preview), version/grade selector, `GrammarCheckPanel` embedded
- View post (`/post/{id:int}`): simplified read-only preview using `Markdig.Markdown.ToHtml()` directly; Save button updates `LastFix` or `LastEdit` timestamp

### Category Management (`/categories`)

`CategoryTree.razor` + `CategoryTreeNode.razor` — recursive tree view with inline CRUD (add child, rename, delete).

### Interactive Exercises (`/exercises`, `/exercises/{PostId:int}/new|edit/{ExerciseId:int}`)

`ExerciseEditor.razor` + type-specific sub-editors:
- `DragDropEditorComponent.razor`
- `MatchingEditorComponent.razor`
- `TimelineEditorComponent.razor`
- `CircuitEditorComponent.razor`

`AllExercises.razor` — list across all posts.

### Questions & Answers

`QuestionsManager.razor` — embedded in `EditPost`. Create/edit multiple-choice questions and answers per post. Supports single-correct and multi-correct answer types.

### Addendums (`/addendums`)

Add/edit/delete addendums. In post Markdown, reference as `(--DOD-{id}--)` — rendered as inline click-to-expand note on PostDetail.

### Related Posts (`/related-posts`, `add`, `edit/{Id:int}`)

Manage `RelatedPost` links between posts. Displayed at bottom of PostDetail.

### Events / Calendar (`/events`)

CRUD for `Event` entities shown on the student `/calendar` page.

### Feedback (`/feedbacks`)

Paginated list of `Feedback` submissions from students.

### Image Upload (`/images`)

Upload images to the file server.

### Grades (`/grades`)

Manage `Grade` entities (name + level integer). Deletion blocked if any PostVersion uses the grade.

### Post Statistics (`/post-stats`)

Per-post difficulty ratings (Easy/Medium/Hard counts), searchable and sortable.

---

## 15. CSS Architecture

Located in `Tobiso.Web.App/wwwroot/css/`. Zero CSS frameworks — entirely custom.

### File Structure

| File | Purpose |
|------|---------|
| `style.css` | Master entry point — imports Google Fonts + all other CSS files |
| `variables.css` | All design tokens as CSS custom properties |
| `grid.css` | Responsive subject card grid |
| `footer.css` | Footer with sidebar-offset margin |
| `index-style.css` | Hero section / homepage-specific styles |
| `tabulkad.css` | Post content table styles |
| `focus-accessibility.css` | Focus ring styles |
| `app.css` | Minimal Blazor error UI scaffolding |

Plus Blazor-scoped CSS: `MainLayout.razor.css`, `NavMenu.razor.css` (sidebar transitions), `Home.razor.css`.

### Design Tokens (`variables.css`)

All values on `:root` as `--color-*` canonical names:
- Typography: `--font-family-sans` (Poppins), `--font-family-serif` (Zilla Slab), `--font-family-monospace` (JetBrains Mono)
- Brand: `--color-accent` (#d175a6 pink), `--color-primary` (#c36f9a), `--color-secondary` (#d89dbd)
- Backgrounds/text: `--color-bg`, `--color-text` with `-dark` variants
- State: `--good`, `--danger-*`, `--warning-*`, `--color-success-*`, `--color-danger-*`
- Shadows: `--shadow-sm/md/lg`, `--backdrop`, `--overlay-bg`
- Semantic: `--color-accent-4` (nav sidebar background), calendar, modal, progress colors

Dark mode is toggled by adding `dark-mode` class to `body` via JS. Components use `--color-*-dark` variants directly.

### Sidebar CSS (`NavMenu.razor.css`)

```css
#MyNavBar { width: 285px; /* expanded */ }
#MyNavBar.collapsed { width: 60px; }
/* transition: width 0.3s ease */
```

Fixed position, left-anchored. Mobile shows a separate slide-in drawer overlay instead.

### Admin CSS

Admin app uses Bootstrap 5 (`wwwroot/lib/bootstrap/`) with no custom design system.

---

## 16. App Bootstrap & DI

### `Tobiso.Web.App/Program.cs`

Registration order:

1. **Serilog** from config
2. **QuestPDF** community license
3. **BasicAuthOptions** from `Auth:Basic` config
4. **SQL Server DbContext** (`TobisoDbContext`) via `ConnectionStrings:DefaultConnection`
5. **SmartAuth** policy scheme (Bearer JWT + Basic Auth + TempCookie for Google)
6. **Google OAuth** (optional, requires `Google:ClientId` + `Google:ClientSecret` in config)
7. **Blazor** (`AddRazorComponents().AddInteractiveServerComponents()`) + cascading auth state
8. **StudentCredentialStore** (Singleton) + **StudentAuthStateProvider** (Scoped)
9. **Domain services** (all Scoped): `ICategoryService`, `IPostService`, `IPostVersionService`, `IGradeService`, `IQuestionService`, `IExplanationService`, `IEventService`, `IRelatedPostService`, `IAddendumService`, `IPushNotificationService`, `IDeviceService`, `IFeedbackService`, `IInteractiveExerciseService`, `IPdfService`
10. **UI services** (Scoped): `AddendumModalService`, `PostsGraphModalService`, `PersonModalService`
11. **AI services**: `IAiRateLimitService` (Singleton), `IAiService` (Scoped), named `HttpClient("OpenAI")`
12. **MVC controllers** (filtered to exclude `Tobiso.Web.Api` assembly to prevent Swagger conflicts), JSON enum string converter
13. **JwtTokenService**, `IUserService`, `IAiChatHistoryService`, `IUserProgressService`, `PdfJsInterop`, `HttpLoggingHandler`
14. **Refit clients**: `ITobisoAnonymApi` + `ITobisoWebApi` with `Api:BaseAddress` config + `HttpLoggingHandler`
15. **Swagger** (dev only) with Basic Auth security definition
16. **Firebase Admin SDK** (optional, from `Auth:Firebase:CredentialsPath`)

Middleware: Swagger → ExceptionHandler/HSTS → static files → routing → auth → antiforgery → `MapControllers()` → `MapRazorComponents<App>()`

### `Tobiso.Web.App.Admin/Program.cs`

Simpler — no AI services, no student auth, no Google OAuth, no Firebase:

1. Serilog, BasicAuthOptions
2. Raw `HttpClient` (Scoped, configurable base address)
3. SQL Server DbContext
4. SmartAuth (JWT + Basic only)
5. Blazor + cascading auth state
6. `TokenAuthenticationStateProvider` (reads admin JWT from localStorage)
7. Same domain services as App (minus AI, PDF, user-specific, modal services)
8. `CredentialStore` (Singleton), `AuthenticationHeaderHandler` + `HttpLoggingHandler` (Transient)
9. Refit `ITobisoWebApi` with `AuthenticationHeaderHandler` (auto-injects Bearer token)
10. Swagger
11. **401-redirect middleware**: any 401 not to `/login` or `/_` → redirect to `/login`

---

## 17. Routing Reference

### Public App (`Tobiso.Web.App`)

| Route | Component | Notes |
|-------|-----------|-------|
| `/` | `Home.razor` | Landing: subject grid, article-of-the-day |
| `/categories/{parentId?}` | `CategoryList.razor` | Browse subjects |
| `/post/{Id:int}` | `PostDetail.razor` | Article by ID |
| `/post/{Slug}` | `PostDetail.razor` | Article by slug |
| `/latest` | `LatestPosts.razor` | Newest articles |
| `/chat` | `Chat.razor` | Standalone AI chat |
| `/questions/{PostId:int}` | `Questions.razor` | Questions for a post |
| `/all-questions` | `AllQuestions.razor` | All quiz questions |
| `/practice` | `Practice.razor` | Practice mode |
| `/calendar` | `Calendar.razor` | Events calendar |
| `/zalozky` | `Bookmarks.razor` | Saved articles |
| `/feedback` | `Feedback.razor` | Submit feedback |
| `/profil` | `Profile.razor` | Student profile (auth required) |
| `/prihlaseni` | `Login.razor` | Student login (EmptyLayout) |
| `/registrace` | `Register.razor` | Student registration (EmptyLayout) |
| `/google-auth-complete` | `GoogleAuthComplete.razor` | Google OAuth landing |
| `/Error` | `Error.razor` | Error page |

### Admin App (`Tobiso.Web.App.Admin`)

| Route | Component |
|-------|-----------|
| `/` | `Home.razor` (post list) |
| `/login` | `Login.razor` |
| `/posts/add` | `AddPost.razor` |
| `/posts/edit/{Id:int}` | `EditPost.razor` |
| `/post/{id:int}` | `PostDetail.razor` |
| `/categories` | `CategoryTree.razor` |
| `/events` | `EventManagement.razor` |
| `/addendums` | `AddendumsManagement.razor` |
| `/related-posts` | `RelatedPosts.razor` |
| `/related-posts/add` | `AddRelatedPost.razor` |
| `/related-posts/edit/{Id:int}` | `EditRelatedPost.razor` |
| `/exercises` | `AllExercises.razor` |
| `/exercises/{PostId:int}/new` | `ExerciseEditor.razor` |
| `/exercises/{PostId:int}/edit/{ExerciseId:int}` | `ExerciseEditor.razor` |
| `/feedbacks` | `FeedbackList.razor` |
| `/images` | `ImageUpload.razor` |
| `/grades` | `Grades.razor` |
| `/post-stats` | `PostStats.razor` |
| `/Error` | `Error.razor` |

---

*Generated from codebase exploration. Last updated: 2026-09-11.*
