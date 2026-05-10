# API Over-Fetching Audit

**Date:** 2026-05-10  
**Scope:** Full-stack — backend services, API controllers, frontend user app (`Tobiso.Web.App`), frontend admin app (`Tobiso.Web.App.Admin`)

---

## Summary

Multiple layers of the application consistently load more data than needed. The most severe pattern is that `PostService.GetAll()` loads the full `Content` field (full Markdown body) for every post on every list call, and this endpoint is used in contexts where only `Id` and `Title` are ever consumed. This single issue cascades into every page that relies on post lists: sidebar, home, latest posts, post navigation, sitemap, and admin views.

---

## Critical

### 1. `PostService.GetAll()` — `Content` loaded for every post in every list call

**File:** `Tobiso.Web.Api/Services/PostService.cs:32`

```csharp
_context.Posts.ToListAsync()  // no .Select() projection
```

`Content` is the full Markdown article body. It is loaded for every single post whenever the list is fetched, regardless of whether the caller needs it. This is the root cause behind several issues below.

**Fix:** Add a dedicated `GetSummaries()` query with a `.Select()` projection returning only `Id`, `Title`, `CategoryId`, `FilePath`, `LastEdit`, `LastFix`. Reserve the full `Content` load for `GetById` calls only.

---

### 2. `PostDetail.razor` — `GetAllPosts()` called twice on every navigation

**File:** `Tobiso.Web.App/Components/Pages/PostDetail.razor:398,413`

`await Api.GetAllPosts()` is called in both `OnInitializedAsync` and `OnParametersSetAsync`. This means every time the user navigates between posts, the entire post list (including full `Content`) is re-fetched from scratch. The result is only used for:

- Resolving titles of related posts (line 339) — needs `Id` + `Title` only
- Finding posts in the same category (line 657) — needs `Id` + `Title` + `CategoryId` only

**Fix:** Switch both calls to `GetPostLinks()` (already exists in `ITobisoWebApi`). Consider caching the result for the duration of the session.

---

### 3. `RightSidebar.razor` — `GetAllPosts()` for `Id` and `Title` only

**File:** `Tobiso.Web.App/Components/RightSidebar.razor:205`

```csharp
var posts = (await Api.GetAllPosts()).ToList();
```

The sidebar only reads `post.Id` and `post.Title` (lines 354–355). The full post list including all `Content` is loaded on every sidebar render.

**Fix:** Replace with `Api.GetPostLinks()` which is already available and projects only the minimal fields.

---

### 4. `QuestionService.GetAll()` — full eager load with no pagination

**File:** `Tobiso.Web.Api/Services/QuestionService.cs:31-35`

```csharp
_context.Questions
    .Include(q => q.Answers)
    .Include(q => q.Explanations)
    .Include(q => q.Post)
    .ToListAsync()
```

All questions with all related entities are loaded at once. `AllQuestions.razor` and `Practice.razor` then filter and paginate entirely client-side. A `GetQuestionsByPostId` endpoint already exists but is underused.

**Fix:** Add server-side `skip`/`take` pagination and a `?postId=` filter parameter to the endpoint. Move filtering logic to the database query.

---

## High

### 5. `PostDetail.razor` — `GetAllCategories()` called twice for a breadcrumb

**File:** `Tobiso.Web.App/Components/Pages/PostDetail.razor:399,414`

`await Api.GetAllCategories()` is called in both lifecycle methods. The full category list is used only to build a 2–3 item breadcrumb trail (`BuildBreadcrumb`, line 783).

**Fix:** Include the ancestor chain directly in the `PostResponse` DTO, or add a `GET /api/Categories/ancestors/{categoryId}` endpoint that returns only the ancestor nodes for a given category.

---

### 6. `LatestPosts.razor` — `GetAllPosts()` for a title/date list with no pagination

**File:** `Tobiso.Web.App/Components/Pages/LatestPosts.razor:82`

```csharp
Api.GetAllPosts()
```

Only `Id`, `Title`, `CategoryId`, `LastEdit`, and `LastFix` are used. The full post list including `Content` is fetched and the entire ordered list is rendered at once with no pagination.

**Fix:** Use a lightweight summary endpoint. Add server-side pagination (e.g. 30 most recent, load more on scroll or paging).

---

### 7. `SitemapController` — `GetAllPosts()` for URL generation only

**File:** `Tobiso.Web.App/Controllers/SitemapController.cs:33`

```csharp
await _api.GetAllPosts()
```

The sitemap only needs the post `Id` to construct URLs. Full `Content` is loaded on every `sitemap.xml` request.

**Fix:** Use `GetPostLinks()` or add a minimal `GET /api/Posts/ids` endpoint. Cache the sitemap output with an appropriate TTL.

---

### 8. `AddendumService.GetAll()` — `Content` loaded for admin list view

**File:** `Tobiso.Web.Api/Services/AddendumService.cs:33`

```csharp
_context.Addendums.ToListAsync()
```

`AddendumsManagement.razor` uses this for display and client-side search. `Content` (full Markdown body) is only needed when editing a single addendum.

**Fix:** Project to `Id` + `Title` + short excerpt for the list endpoint. Load full `Content` only on `GetById`.

---

### 9. `EventManagement.razor` (admin) — client-side filtering when server-side endpoints exist

**File:** `Tobiso.Web.App.Admin/Components/Pages/EventManagement.razor`

All events are loaded and then filtered client-side by date range and search input. A `GET /api/Events/range` endpoint and a `SearchEvents` endpoint already exist.

**Fix:** Wire the date range and search inputs to the existing server-side endpoints. Fetch only the events matching the current filter.

---

### 10. `RelatedPosts.razor` (admin) — loads all posts to resolve two titles per row

**File:** `Tobiso.Web.App.Admin/Components/Pages/RelatedPosts.razor`

The full post list is loaded to do `.FirstOrDefault(p => p.Id == rp.PostId)` for display purposes.

**Fix:** Include `PostTitle` and `RelatedPostTitle` directly in the `RelatedPostResponse` DTO via a backend join. Eliminates the need to load all posts on the frontend.

---

### 11. `EditRelatedPost.razor` (admin) — loads all post links to pre-select two entries

**File:** `Tobiso.Web.App.Admin/Components/Pages/EditRelatedPost.razor:129`

```csharp
Api.GetPostLinks()
```

The full post link list is fetched only to find two entries by ID (lines 144–145) and pre-populate the form.

**Fix:** Pre-populate the two `PostSearchComponent`s directly from the already-loaded `RelatedPostResponse` data. Only fetch more on explicit user search.

---

### 12. `InteractiveExerciseService` — N+1 query for category ancestor traversal

**File:** `Tobiso.Web.Api/Services/InteractiveExerciseService.cs:41-50`

A `while` loop issues one `await _context.Categories.Where(...).FirstOrDefaultAsync()` per ancestor level, resulting in one database round-trip per category depth.

**Fix:** Load all categories once into a `Dictionary<int, Category>` before the loop, then traverse the ancestor chain in memory.

---

## Medium

### 13. `CategoryService.GetTree()` — full entity load for tree building

**File:** `Tobiso.Web.Api/Services/CategoryService.cs:40`

```csharp
_context.Categories.ToListAsync()
```

Building the category tree only needs `Id`, `Name`, and `ParentId`. All other columns are loaded unnecessarily.

**Fix:**
```csharp
_context.Categories.Select(c => new { c.Id, c.Name, c.ParentId }).ToListAsync()
```

---

### 14. No pagination on Feedback, Event, and RelatedPost list endpoints

**Files:**
- `Tobiso.Web.Api/Services/FeedbackService.cs`
- `Tobiso.Web.Api/Services/EventService.cs`
- `Tobiso.Web.Api/Services/RelatedPostService.cs`

All return unbounded lists. Admin pages render everything at once.

**Fix:** Add `page` / `pageSize` parameters to both the service methods and API endpoints. Update admin tables to support paging controls.

---

### 15. Admin exercise pages load full posts and categories for dropdowns

**Files:**
- `Tobiso.Web.App.Admin/Components/Pages/AllExercises.razor`
- `Tobiso.Web.App.Admin/Components/Pages/ExerciseEditor.razor`

Full post and category lists are loaded to populate dropdowns and lookup controls. Only `Id`, `Title`, and `CategoryId` are needed.

**Fix:** Switch to `GetPostLinks()` (already exists and returns the minimal fields). For categories, use the same projected query recommended in item 13.

---

## Recommended Priority Order

| Priority | Action |
|---|---|
| 1 | Add a `GetPostSummaries()` backend endpoint that excludes `Content` and use it everywhere except `GetById` |
| 2 | Replace all `GetAllPosts()` calls in `RightSidebar`, `LatestPosts`, `SitemapController`, and `PostDetail` with the lightweight endpoint |
| 3 | Add server-side pagination + filtering to the Questions endpoint; remove client-side pagination |
| 4 | Fix the N+1 ancestor loop in `InteractiveExerciseService` |
| 5 | Include ancestor chain in `PostResponse` to eliminate the all-categories fetch in `PostDetail` |
| 6 | Wire `EventManagement` admin filters to existing server-side endpoints |
| 7 | Add `PostTitle`/`RelatedPostTitle` to `RelatedPostResponse` DTO |
| 8 | Add pagination to Feedback, Event, and RelatedPost admin endpoints |
| 9 | Apply `.Select()` projections in `CategoryService` and `AddendumService` |
