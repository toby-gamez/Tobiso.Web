namespace Tobiso.Api.Infrastructure.Data;

using Tobiso.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TobisoDbContext : DbContext
{
    public TobisoDbContext(DbContextOptions<TobisoDbContext> options)
        : base(options) { }

   public DbSet<Category> Categories { get; set; }
    public DbSet<Post> Posts { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}