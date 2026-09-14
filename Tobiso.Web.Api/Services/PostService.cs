using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Shared.Helpers;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IPostService
{
    /// <summary>Returns all posts. When gradeId is supplied, Content/LastFix/LastEdit come from the best-matching version.</summary>
    Task<List<PostResponse>> GetAll(int? gradeId = null);
    Task<List<PostSummaryResponse>> GetSummaries();
    Task<List<PostLinkResponse>> GetLinks();
    /// <summary>Full-text search across post version content. One result per matching post (its first matching version), with the matched phrase plus the word before/after it for a preview snippet.</summary>
    Task<List<PostSearchResultDto>> SearchContentAsync(string query, int take = 10);
    /// <summary>Returns a single post including all its versions. When gradeId is supplied the top-level Content fields reflect the best match.</summary>
    Task<PostResponse?> GetById(int id, int? gradeId = null);
    /// <summary>Looks up a post by its URL slug (see <see cref="PostSlug"/>). Returns null if no post's slug matches.</summary>
    Task<PostResponse?> GetBySlug(string slug, int? gradeId = null);
    /// <summary>Updates post metadata only (title, filepath, category). Version content is managed via IPostVersionService.</summary>
    Task<bool> UpdateMetadata(int id, UpdatePostRequest req);
    Task<bool> Delete(int id);
    Task<PostResponse?> Create(CreatePostRequest req);
    Task<PostLinkResponse?> GetRandomAsync();
    Task<PostLinkResponse?> GetArticleOfTheDayAsync();
}

public class PostService : IPostService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<PostService> _logger;

    public PostService(TobisoDbContext context, ILogger<PostService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static PostVersionResponse ToVersionResponse(PostVersion v) => new()
    {
        Id = v.Id,
        PostId = v.PostId,
        GradeId = v.GradeId,
        GradeName = v.Grade?.Name,
        GradeLevel = v.Grade?.Level,
        Content = v.Content,
        LastFix = v.LastFix,
        LastEdit = v.LastEdit
    };

    /// <summary>
    /// Picks the best version for the given preferred grade level:
    /// highest Grade.Level that is ≤ preferredLevel, falling back to the highest available.
    /// </summary>
    private static PostVersion? BestMatch(IEnumerable<PostVersion> versions, int preferredLevel)
    {
        var candidates = versions
            .Where(v => v.Grade != null && v.Grade.Level <= preferredLevel)
            .OrderByDescending(v => v.Grade!.Level)
            .FirstOrDefault();

        return candidates
            ?? versions.OrderByDescending(v => v.Grade?.Level ?? int.MinValue).FirstOrDefault();
    }

    /// <summary>
    /// Builds a PostResponse. <paramref name="versionsToInclude"/> determines what goes into Versions[]:
    /// pass a single best-match version when gradeId was specified; pass all versions otherwise.
    /// </summary>
    private static PostResponse BuildResponse(Post p, IEnumerable<PostVersion> versionsToInclude)
    {
        return new PostResponse
        {
            Id = p.Id,
            Title = p.Title,
            FilePath = p.FilePath,
            CategoryId = p.CategoryId,
            Versions = versionsToInclude.Select(ToVersionResponse).ToList()
        };
    }

    // ── IPostService ─────────────────────────────────────────────────────────

    public async Task<List<PostResponse>> GetAll(int? gradeId = null)
    {
        try
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Grade)
                .ToListAsync();

            int? preferredLevel = null;
            if (gradeId.HasValue)
            {
                var grade = await _context.Grades.FindAsync(gradeId.Value);
                if (grade == null) return new List<PostResponse>();
                preferredLevel = grade.Level;
            }

            var result = new List<PostResponse>();
            foreach (var p in posts)
            {
                if (preferredLevel.HasValue)
                {
                    var matched = BestMatch(p.Versions, preferredLevel.Value);
                    // Skip posts with no versions when a grade filter is active
                    if (matched == null) continue;
                    result.Add(BuildResponse(p, new[] { matched }));
                }
                else
                {
                    result.Add(BuildResponse(p, p.Versions));
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading posts");
            throw;
        }
    }

    public async Task<List<PostSummaryResponse>> GetSummaries()
    {
        return await _context.Posts
            .Select(p => new PostSummaryResponse
            {
                Id = p.Id,
                Title = p.Title,
                CategoryId = p.CategoryId,
                FilePath = p.FilePath,
                // Aggregate most recent timestamps across all versions for "last updated" display
                LastEdit = p.Versions.Max(v => (DateTime?)v.LastEdit),
                LastFix  = p.Versions.Max(v => (DateTime?)v.LastFix),
                AvailableGradeNames = p.Versions
                    .Where(v => v.Grade != null)
                    .OrderBy(v => v.Grade!.Level)
                    .Select(v => v.Grade!.Name)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<PostLinkResponse>> GetLinks()
    {
        return await _context.Posts
            .Select(p => new PostLinkResponse
            {
                Id = p.Id,
                Title = p.Title,
                FilePath = p.FilePath
            })
            .ToListAsync();
    }

    public async Task<PostResponse?> GetById(int id, int? gradeId = null)
    {
        var post = await _context.Posts
            .AsNoTracking()
            .Include(p => p.Versions)
                .ThenInclude(v => v.Grade)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return null;

        if (gradeId.HasValue)
        {
            var grade = await _context.Grades.FindAsync(gradeId.Value);
            if (grade != null)
            {
                var matched = BestMatch(post.Versions, grade.Level);
                // Return only the matched version in Versions[]
                return BuildResponse(post, matched != null ? new[] { matched } : Array.Empty<PostVersion>());
            }
        }

        // No gradeId: return all versions
        return BuildResponse(post, post.Versions);
    }

    public async Task<PostResponse?> GetBySlug(string slug, int? gradeId = null)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        // No dedicated slug column - match by deriving the slug from FilePath in memory.
        var candidates = await _context.Posts
            .AsNoTracking()
            .Select(p => new { p.Id, p.FilePath })
            .ToListAsync();

        var match = candidates.FirstOrDefault(p =>
            string.Equals(PostSlug.FromFilePath(p.FilePath), slug, StringComparison.OrdinalIgnoreCase));

        return match == null ? null : await GetById(match.Id, gradeId);
    }

    public async Task<bool> UpdateMetadata(int id, UpdatePostRequest req)
    {
        var entity = await _context.Posts.FindAsync(id);
        if (entity == null) return false;

        entity.Title = req.Title;
        entity.FilePath = req.FilePath;
        entity.CategoryId = req.CategoryId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            // Remove any RelatedPost entries that reference this post as RelatedPostId
            var relatedRefs = await _context.RelatedPosts
                .Where(r => r.RelatedPostId == id)
                .ToListAsync();
            if (relatedRefs.Any())
                _context.RelatedPosts.RemoveRange(relatedRefs);

            // Remove any AI chat attachments referencing this post - its AiChatSessionPosts FK is
            // NoAction (SQL Server forbids it being cascade), so the join rows need manual cleanup.
            var chatAttachedRefs = await _context.AiChatSessionPosts
                .Where(x => x.PostId == id)
                .ToListAsync();
            if (chatAttachedRefs.Count > 0)
                _context.AiChatSessionPosts.RemoveRange(chatAttachedRefs);

            var entity = await _context.Posts
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return false;

            _context.Posts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            throw;
        }
    }

    public async Task<PostLinkResponse?> GetRandomAsync()
    {
        var count = await _context.Posts.CountAsync();
        if (count == 0) return null;
        var skip = Random.Shared.Next(count);
        return await _context.Posts
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Select(p => new PostLinkResponse { Id = p.Id, Title = p.Title, FilePath = p.FilePath })
            .FirstOrDefaultAsync();
    }

    public async Task<PostLinkResponse?> GetArticleOfTheDayAsync()
    {
        var posts = await _context.Posts
            .OrderBy(p => p.Id)
            .Select(p => new PostLinkResponse { Id = p.Id, Title = p.Title, FilePath = p.FilePath })
            .ToListAsync();
        if (posts.Count == 0) return null;
        return posts[DateTime.UtcNow.DayOfYear % posts.Count];
    }

    public async Task<PostResponse?> Create(CreatePostRequest req)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var entity = new Post
        {
            Title = req.Title,
            FilePath = req.FilePath,
            CategoryId = req.CategoryId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Posts.Add(entity);
        await _context.SaveChangesAsync();

        if (req.GradeId.HasValue)
        {
            var now = DateTime.UtcNow;
            _context.PostVersions.Add(new PostVersion
            {
                PostId = entity.Id,
                GradeId = req.GradeId.Value,
                Content = req.Content,
                LastFix = req.IsFix ? now : null,
                LastEdit = req.IsFix ? null : now
            });
            await _context.SaveChangesAsync();
        }

        await tx.CommitAsync();
        return await GetById(entity.Id);
    }

    public async Task<List<PostSearchResultDto>> SearchContentAsync(string query, int take = 10)
    {
        var term = query?.Trim() ?? "";
        if (term.Length < 2) return new List<PostSearchResultDto>();

        // Over-fetch versions (several posts may have multiple matching grade versions);
        // we only keep the first match per post below.
        var candidates = await _context.PostVersions
            .Include(v => v.Post)
            .Include(v => v.Grade)
            .Where(v => v.Content.Contains(term))
            .OrderBy(v => v.PostId)
            .Take(take * 5)
            .ToListAsync();

        var results = new List<PostSearchResultDto>();
        var seenPostIds = new HashSet<int>();
        foreach (var v in candidates)
        {
            if (v.Post == null || !seenPostIds.Add(v.PostId)) continue;

            var snippet = ExtractSnippet(v.Content, term);
            if (snippet == null) continue;

            results.Add(new PostSearchResultDto
            {
                PostId = v.PostId,
                Title = v.Post.Title,
                CategoryId = v.Post.CategoryId,
                GradeId = v.GradeId,
                GradeName = v.Grade?.Name,
                WordBefore = snippet.Value.WordBefore,
                MatchWord = snippet.Value.MatchWord,
                WordAfter = snippet.Value.WordAfter
            });

            if (results.Count >= take) break;
        }

        return results;
    }

    // ── content snippet extraction ─────────────────────────────────────────

    private static readonly Regex CodeFence = new(@"```[\s\S]*?```", RegexOptions.Compiled);
    private static readonly Regex InlineCode = new(@"`([^`]*)`", RegexOptions.Compiled);
    private static readonly Regex ImageOrLink = new(@"!?\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
    private static readonly Regex LineMarker = new(@"(?m)^[ \t]*[#>\-\*\+]+[ \t]*", RegexOptions.Compiled);
    private static readonly Regex EmphasisMarker = new(@"[*_~]{1,3}", RegexOptions.Compiled);
    private static readonly Regex TablePipe = new(@"\|", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"\S+", RegexOptions.Compiled);
    private static readonly char[] TokenTrimChars = "#*_~`>|[]()\"'.,;:!?".ToCharArray();

    /// <summary>Strips common markdown syntax so word boundaries read naturally for a search snippet.</summary>
    private static string StripMarkdown(string markdown)
    {
        var s = CodeFence.Replace(markdown, " ");
        s = InlineCode.Replace(s, "$1");
        s = ImageOrLink.Replace(s, "$1");
        s = LineMarker.Replace(s, "");
        s = EmphasisMarker.Replace(s, "");
        s = TablePipe.Replace(s, " ");
        return s;
    }

    private static (string? WordBefore, string MatchWord, string? WordAfter)? ExtractSnippet(string content, string term)
    {
        // Locate the match on raw content first (this is what the DB Contains() matched on),
        // then take a window around it wide enough to survive markdown stripping.
        var rawIndex = content.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (rawIndex < 0) return null;

        var windowStart = Math.Max(0, rawIndex - 200);
        var windowEnd = Math.Min(content.Length, rawIndex + term.Length + 200);
        var window = StripMarkdown(content[windowStart..windowEnd]);

        var matchIndex = window.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0) return null; // term only existed inside stripped markdown syntax (e.g. a URL)

        var tokens = Token.Matches(window);
        if (tokens.Count == 0) return null;

        var matchEnd = matchIndex + term.Length;
        var startTokenIdx = -1;
        var endTokenIdx = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (startTokenIdx < 0 && t.Index + t.Length > matchIndex) startTokenIdx = i;
            if (t.Index < matchEnd) endTokenIdx = i;
        }
        if (startTokenIdx < 0 || endTokenIdx < 0) return null;

        var matchWord = window[tokens[startTokenIdx].Index..(tokens[endTokenIdx].Index + tokens[endTokenIdx].Length)].Trim();
        if (matchWord.Length == 0) return null;

        var wordBefore = startTokenIdx > 0 ? tokens[startTokenIdx - 1].Value.Trim(TokenTrimChars) : null;
        var wordAfter = endTokenIdx < tokens.Count - 1 ? tokens[endTokenIdx + 1].Value.Trim(TokenTrimChars) : null;

        return (
            string.IsNullOrEmpty(wordBefore) ? null : wordBefore,
            matchWord,
            string.IsNullOrEmpty(wordAfter) ? null : wordAfter
        );
    }
}
