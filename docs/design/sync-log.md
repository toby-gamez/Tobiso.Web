repo: toby-gamez/Tobiso.Web
branch: main

## Last sync

date: 2026-09-11T13:05:00Z
commit: 5577986

### Updated in this project

- Read ARCHITECTURE.md (added in 5577986) as the source for screens, routes and the post-rendering pipeline.
- Redesigned the app shell in two variants (left rail / top bar) on the Organic design system with the pink accent kept.
- Rebuilt the post page: toolbar, grade badge + version picker, markdown/math/image renderer, callouts, exercises, AI chat, related posts.

## Screen map

| Project screen | Repo files |
| --- | --- |
| Tobiso.dc.html — shell 1a / 1b | ARCHITECTURE.md §13, Components/Layout/MainLayout.razor, Components/Layout/NavMenu.razor.css, wwwroot/css/variables.css, Components/Pages/Home.razor |
| PostBody.dc.html — post page | ARCHITECTURE.md §3–5, Components/Pages/PostDetail.razor, Components/MarkdownContent.razor |
