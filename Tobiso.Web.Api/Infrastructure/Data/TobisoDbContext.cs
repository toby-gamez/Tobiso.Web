namespace Tobiso.Api.Infrastructure.Data;

using Tobiso.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TobisoDbContext : DbContext
{
    public TobisoDbContext(DbContextOptions<TobisoDbContext> options)
        : base(options) { }

   public DbSet<Category> Categories { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }


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
    }
}