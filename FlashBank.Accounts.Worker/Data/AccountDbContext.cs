using FlashBank.Accounts.Worker.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Accounts.Worker.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options)
        : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");

            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");

            entity.Property(a => a.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(a => a.Balance)
                .HasColumnName("balance")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(a => a.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        });
    }
}
