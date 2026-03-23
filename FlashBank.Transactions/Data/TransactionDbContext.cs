using FlashBank.Shared.Enums;
using FlashBank.Transactions.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashBank.Transactions.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");

            entity.Property(t => t.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            entity.Property(t => t.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Almacenar enum como string para respetar los CHECK constraints del SQL
            entity.Property(t => t.Type)
                .HasColumnName("type")
                .HasMaxLength(50)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<TransactionType>(v));

            entity.Property(t => t.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<TransactionStatus>(v));

            entity.Property(t => t.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        });
    }
}
