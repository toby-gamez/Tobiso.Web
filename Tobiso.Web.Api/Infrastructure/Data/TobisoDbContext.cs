namespace Tobiso.Api.Infrastructure.Data;

using Tobiso.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TobisoDbContext : DbContext
{
    public TobisoDbContext(DbContextOptions<TobisoDbContext> options)
        : base(options) { }

    // Bumps ChronicleCacheVersion whenever a kronika entity changes, so ChronicleAxisService's
    // cached layouts invalidate without every Chronicle* service having to call it manually.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var touchedChronicle = ChangeTrackerHasChronicleChanges();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        if (touchedChronicle) Tobiso.Web.Api.Services.ChronicleCacheVersion.Bump();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var touchedChronicle = ChangeTrackerHasChronicleChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (touchedChronicle) Tobiso.Web.Api.Services.ChronicleCacheVersion.Bump();
        return result;
    }

    private bool ChangeTrackerHasChronicleChanges() =>
        ChangeTracker.Entries().Any(e =>
            e.State != EntityState.Unchanged &&
            e.Entity.GetType().Name.StartsWith("Chronicle", StringComparison.Ordinal));

    public DbSet<Category> Categories { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostVersion> PostVersions { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Explanation> Explanations { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<RelatedPost> RelatedPosts { get; set; }
    public DbSet<Addendum> Addendums { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<InteractiveExercise> InteractiveExercises { get; set; }
    public DbSet<InteractiveExercisePost> InteractiveExercisePosts { get; set; }
    public DbSet<InteractiveExerciseCategory> InteractiveExerciseCategories { get; set; }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<AiChatSession> AiChatSessions { get; set; }
    public DbSet<AiChatMessage> AiChatMessages { get; set; }
    public DbSet<AiChatSessionPost> AiChatSessionPosts { get; set; }
    public DbSet<AiCreditTransaction> AiCreditTransactions { get; set; }
    public DbSet<UserBookmark> UserBookmarks { get; set; }
    public DbSet<UserReadPost> UserReadPosts { get; set; }
    public DbSet<UserNote> UserNotes { get; set; }
    public DbSet<QuestionAttempt> QuestionAttempts { get; set; }
    public DbSet<AnonymousAiUsage> AnonymousAiUsages { get; set; }

    public DbSet<PostFunFact> PostFunFacts { get; set; }
    public DbSet<PostDifficultyRating> PostDifficultyRatings { get; set; }
    public DbSet<PostVideo> PostVideos { get; set; }

    public DbSet<PostKeyTerms> PostKeyTerms { get; set; }
    public DbSet<PostConceptMap> PostConceptMaps { get; set; }
    public DbSet<PostCrossConnection> PostCrossConnections { get; set; }
    public DbSet<PostAiDemo> PostAiDemos { get; set; }
    public DbSet<PostRelatedSuggestion> PostRelatedSuggestions { get; set; }
    public DbSet<PostExamSummary> PostExamSummaries { get; set; }

    // Kronika (interactive history timeline)
    public DbSet<ChronicleItem> ChronicleItems { get; set; }
    public DbSet<ChronicleEvent> ChronicleEvents { get; set; }
    public DbSet<ChroniclePerson> ChroniclePersons { get; set; }
    public DbSet<ChronicleCategory> ChronicleCategories { get; set; }
    public DbSet<ChronicleItemCategory> ChronicleItemCategories { get; set; }
    public DbSet<ChronicleItemLink> ChronicleItemLinks { get; set; }
    public DbSet<ChronicleEventPerson> ChronicleEventPersons { get; set; }
    public DbSet<ChroniclePeriodization> ChroniclePeriodizations { get; set; }
    public DbSet<ChroniclePeriod> ChroniclePeriods { get; set; }
    public DbSet<ChronicleRegion> ChronicleRegions { get; set; }
    public DbSet<ChronicleItemRegion> ChronicleItemRegions { get; set; }
    public DbSet<ChroniclePolity> ChroniclePolities { get; set; }
    public DbSet<ChroniclePolityTerritory> ChroniclePolityTerritories { get; set; }
    public DbSet<ChronicleAxis> ChronicleAxes { get; set; }
    public DbSet<ChronicleAxisEntry> ChronicleAxisEntries { get; set; }
    public DbSet<ChronicleSource> ChronicleSources { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Question entity
        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.QuestionText)
                .HasColumnName("Question")
                .IsRequired()
                .HasMaxLength(200);
                
            entity.HasOne(e => e.Post)
                .WithMany(p => p.Questions)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Answer entity
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.Property(e => e.AnswerText)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Correct)
                .IsRequired();
                
            entity.HasOne(e => e.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Explanation entity
        modelBuilder.Entity<Explanation>(entity =>
        {
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.HasOne(e => e.Question)
                .WithMany(q => q.Explanations)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.Location)
                .HasMaxLength(200);
                
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValue("#007bff");
                
            entity.Property(e => e.RecurrencePattern)
                .HasMaxLength(50);
                
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure RelatedPost entity
        modelBuilder.Entity<RelatedPost>(entity =>
        {
            entity.Property(e => e.Text)
                .HasMaxLength(500);
                
            // Konfigurace relace k hlavnímu postu
            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Konfigurace relace k souvisejícímu postu
            entity.HasOne(e => e.RelatedPostRef)
                .WithMany()
                .HasForeignKey(e => e.RelatedPostId)
                .OnDelete(DeleteBehavior.NoAction); // Zabránit cascade na sobě

            // Unique constraint pro kombinaci PostId a RelatedPostId
            entity.HasIndex(e => new { e.PostId, e.RelatedPostId })
                .IsUnique();
                
            // Konfigurace tabulky s check constraint
            entity.ToTable(t => t.HasCheckConstraint("CK_RelatedPost_DifferentPosts", "[PostId] <> [RelatedPostId]"));
        });

        // Configure PostVersion
        modelBuilder.Entity<PostVersion>(entity =>
        {
            entity.HasOne(v => v.Post)
                .WithMany(p => p.Versions)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Grade)
                .WithMany()
                .HasForeignKey(v => v.GradeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(v => new { v.PostId, v.GradeId }).IsUnique();
            entity.Property(v => v.Content).IsRequired();
        });

        // Configure Grade
        modelBuilder.Entity<Grade>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Level).IsRequired();
            entity.HasIndex(e => e.Level).IsUnique();
        });
        
        // Configure InteractiveExercise entity
        modelBuilder.Entity<InteractiveExercise>(entity =>
        {
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.ConfigJson)
                .IsRequired();
                
            entity.Property(e => e.SolutionJson)
                .IsRequired();
                
            // Legacy single-post FK left optional; main association uses join tables
            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.PostId, e.OrderIndex });
        });

        // Configure join table InteractiveExercisePost
        modelBuilder.Entity<InteractiveExercisePost>(entity =>
        {
            entity.HasKey(e => new { e.InteractiveExerciseId, e.PostId });

            entity.HasOne(e => e.InteractiveExercise)
                .WithMany(x => x.InteractiveExercisePosts)
                .HasForeignKey(e => e.InteractiveExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany(p => p.InteractiveExercisePosts)
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure join table InteractiveExerciseCategory
        modelBuilder.Entity<InteractiveExerciseCategory>(entity =>
        {
            entity.HasKey(e => new { e.InteractiveExerciseId, e.CategoryId });

            entity.HasOne(e => e.InteractiveExercise)
                .WithMany(x => x.InteractiveExerciseCategories)
                .HasForeignKey(e => e.InteractiveExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.InteractiveExerciseCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AppUser
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.GoogleId).HasMaxLength(128);
            entity.Property(e => e.Credits).HasDefaultValue(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.GoogleId).IsUnique()
                .HasFilter("[GoogleId] IS NOT NULL");
        });

        // Configure AnonymousAiUsage - the persistent, per-device counter backing the anonymous
        // free-AI allowance (a one-time lifetime pool, unlike the renewing per-account quota).
        modelBuilder.Entity<AnonymousAiUsage>(entity =>
        {
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.FirstSeenAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastUsedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.DeviceId).IsUnique();
        });

        // Configure AiChatSession
        modelBuilder.Entity<AiChatSession>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(80);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure join table AiChatSessionPost (attached posts per chat session)
        modelBuilder.Entity<AiChatSessionPost>(entity =>
        {
            entity.HasKey(e => new { e.AiChatSessionId, e.PostId });

            entity.HasOne(e => e.AiChatSession)
                .WithMany(s => s.AttachedPosts)
                .HasForeignKey(e => e.AiChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                // NoAction (not Cascade): a cascade here plus the cascade already leaving
                // AiChatSessions for Posts would create two cascade paths to Posts, which SQL
                // Server rejects. Orphaned join rows are cleaned up manually in PostService.Delete.
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure AiChatMessage
        modelBuilder.Entity<AiChatMessage>(entity =>
        {
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AiCreditTransaction
        modelBuilder.Entity<AiCreditTransaction>(entity =>
        {
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.CreditTransactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserBookmark
        modelBuilder.Entity<UserBookmark>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique();
        });

        // Configure UserReadPost
        modelBuilder.Entity<UserReadPost>(entity =>
        {
            entity.Property(e => e.FirstReadAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastReadAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ReadPosts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique();
        });

        // Configure UserNote
        modelBuilder.Entity<UserNote>(entity =>
        {
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Post)
                .WithMany()
                .HasForeignKey(e => e.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.PostId }).IsUnique();
        });

        // Configure QuestionAttempt
        modelBuilder.Entity<QuestionAttempt>(entity =>
        {
            entity.Property(e => e.FirstAttemptedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.LastAttemptedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.QuestionAttempts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.QuestionId }).IsUnique();
        });

        // --- Kronika (interactive history timeline) ---

        // ChronicleItem: TPH base for events and people
        modelBuilder.Entity<ChronicleItem>(entity =>
        {
            entity.HasDiscriminator<string>("ItemType")
                .HasValue<ChronicleEvent>("Event")
                .HasValue<ChroniclePerson>("Person");

            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.ReliabilityNote).HasMaxLength(500);
            entity.Property(e => e.LessonSlug).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => new { e.StartYear, e.EndYear });

            entity.HasOne(e => e.MinGrade)
                .WithMany()
                .HasForeignKey(e => e.MinGradeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ChroniclePerson>(entity =>
        {
            entity.Property(e => e.Occupation).HasMaxLength(200);
        });

        // ChronicleCategory
        modelBuilder.Entity<ChronicleCategory>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Color).HasMaxLength(7);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Join table: ChronicleItemCategory
        modelBuilder.Entity<ChronicleItemCategory>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.CategoryId });

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Categories)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Causality: ChronicleItemLink
        modelBuilder.Entity<ChronicleItemLink>(entity =>
        {
            entity.Property(e => e.Explanation).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.From)
                .WithMany()
                .HasForeignKey(e => e.FromId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction on this side: Cascade on both From and To would be two cascade paths
            // into ChronicleItem, which SQL Server rejects (same pattern as AiChatSessionPost above).
            entity.HasOne(e => e.To)
                .WithMany()
                .HasForeignKey(e => e.ToId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => new { e.FromId, e.ToId, e.Type }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ChronicleItemLink_DifferentItems", "[FromId] <> [ToId]"));
        });

        // Join table: ChronicleEventPerson (role chips)
        modelBuilder.Entity<ChronicleEventPerson>(entity =>
        {
            entity.HasKey(e => new { e.EventId, e.PersonId });
            entity.Property(e => e.Note).HasMaxLength(300);

            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction: same multiple-cascade-path reason as ChronicleItemLink.To above.
            entity.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Periodization / Period
        modelBuilder.Entity<ChroniclePeriodization>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ChroniclePeriod>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Color).HasMaxLength(7);

            entity.HasOne(e => e.Periodization)
                .WithMany(p => p.Periods)
                .HasForeignKey(e => e.PeriodizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StartEvent)
                .WithMany()
                .HasForeignKey(e => e.StartEventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.PeriodizationId, e.Order }).IsUnique();
        });

        // Region (materialized path) / Polity
        modelBuilder.Entity<ChronicleRegion>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(400);

            entity.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Path);
        });

        modelBuilder.Entity<ChronicleItemRegion>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.RegionId });

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Regions)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Region)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChroniclePolity>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<ChroniclePolityTerritory>(entity =>
        {
            entity.HasOne(e => e.Polity)
                .WithMany(p => p.Territories)
                .HasForeignKey(e => e.PolityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Region)
                .WithMany(r => r.PolityTerritories)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.PolityId, e.RegionId });
        });

        // Axis (display definition) / AxisEntry
        modelBuilder.Entity<ChronicleAxis>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<ChronicleAxisEntry>(entity =>
        {
            entity.HasOne(e => e.Axis)
                .WithMany(a => a.Entries)
                .HasForeignKey(e => e.AxisId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint("CK_ChronicleAxisEntry_ItemOrCategory",
                "([ItemId] IS NOT NULL AND [CategoryId] IS NULL) OR ([ItemId] IS NULL AND [CategoryId] IS NOT NULL)"));
        });

        // Sources
        modelBuilder.Entity<ChronicleSource>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(500);

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Sources)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
