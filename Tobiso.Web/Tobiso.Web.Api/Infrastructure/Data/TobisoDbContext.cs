namespace Tobiso.Api.Infrastructure.Data;

using Tobiso.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TobisoDbContext : DbContext
{
    public TobisoDbContext(DbContextOptions<TobisoDbContext> options)
        : base(options) { }

   // public DbSet<Link> Links { get; set; }
   

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
       // modelBuilder.Entity<UrlParam>().ToTable("BlinkedUrlParams");

    }
}