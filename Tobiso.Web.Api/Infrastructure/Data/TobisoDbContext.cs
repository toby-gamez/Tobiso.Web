namespace Tobiso.Api.Infrastructure.Data;

using Tobiso.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TobisoDbContext : DbContext
{
    public TobisoDbContext(DbContextOptions<TobisoDbContext> options)
        : base(options) { }

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
    // Persons removed - AI-generated on demand. Keep code commented for easy rollback if needed.
    // public DbSet<Person> Persons { get; set; }
    // public DbSet<PostPersonMention> PostPersonMentions { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<InteractiveExercise> InteractiveExercises { get; set; }
    public DbSet<InteractiveExercisePost> InteractiveExercisePosts { get; set; }
    public DbSet<InteractiveExerciseCategory> InteractiveExerciseCategories { get; set; }


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
    }
}
