
using MeterReadingApi.DataAccess.Repository.Accounts;
using MeterReadingApi.DataAccess.Repository.MeterValues;
using Microsoft.EntityFrameworkCore;

namespace MeterReadingApi.DataAccess.Repository;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<PersistedAccount> Accounts { get; set; }
    public DbSet<PersistedMeterValue> MeterValues { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PersistedAccount>(entity =>
        {
            entity.HasIndex(e => e.AccountNumber, "IX_AccountNumber").IsUnique();
        });

        builder.Entity<PersistedMeterValue>(entity =>
        {
            entity.HasIndex(e => new { e.AccountId, e.MeterReadingDateTime }, "IX_AccountId_MeterReadingDateTime").IsUnique();
        });
    }
}