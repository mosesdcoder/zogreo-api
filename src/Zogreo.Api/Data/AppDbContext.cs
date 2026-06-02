using Microsoft.EntityFrameworkCore;

namespace Zogreo.Api.Data;

// TODO (Slice 1): Add all DbSets, global query filters, audit stamping.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // TODO: configure entities, indexes, query filters
    }
}
