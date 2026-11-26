using Gcmac.Registration.Models;
using Microsoft.EntityFrameworkCore;

namespace Gcmac.Registration.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<MemberAccount> MemberAccounts => Set<MemberAccount>();
    public DbSet<RegistrationRecord> Registrations => Set<RegistrationRecord>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<NotificationMessage> Notifications => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Email);
            entity.Property(a => a.Email).HasMaxLength(320);
        });

        modelBuilder.Entity<MemberAccount>(entity =>
        {
            entity.HasKey(m => m.MemberNo);
            entity.Property(m => m.MemberNo).HasMaxLength(32);
            entity.Property(m => m.Name).HasMaxLength(160).IsRequired();
            entity.Property(m => m.Mobile).HasMaxLength(32).IsRequired();
            entity.Property(m => m.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(m => m.Email);
        });

        modelBuilder.Entity<RegistrationRecord>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasMaxLength(64);
            entity.Property(r => r.MembershipId).HasMaxLength(128);
            entity.HasIndex(r => r.MembershipId);
            entity.Property(r => r.AccountEmail).HasMaxLength(320).IsRequired();
            entity.Property(r => r.SponsorMemberNo).HasMaxLength(32);
            entity.Property(r => r.PaymentBatchId).HasMaxLength(64);
            entity.HasIndex(r => r.PaymentBatchId);
            entity.Property(r => r.BaseFee).HasPrecision(18, 2);
            entity.Property(r => r.GstRate).HasPrecision(18, 4);
            entity.Property(r => r.GstAmount).HasPrecision(18, 2);
            entity.Property(r => r.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(r => r.AccountEmail);
            entity.HasIndex(r => r.SponsorMemberNo);
            entity.HasIndex(r => r.PaymentStatus);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(t => t.MerchantTxnRefNo);
            entity.Property(t => t.MerchantTxnRefNo).HasMaxLength(64);
            entity.Property(t => t.BatchId).HasMaxLength(64);
            entity.Property(t => t.RegistrationIdsCsv).HasMaxLength(2048);
            entity.Property(t => t.Kind).HasMaxLength(32).IsRequired();
            entity.HasIndex(t => t.BatchId);
            entity.Property(t => t.Method).HasMaxLength(32).IsRequired();
            entity.Property(t => t.Status).HasMaxLength(32).IsRequired();
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.HasIndex(t => t.RegistrationId);
            entity.HasIndex(t => t.Status);
        });

        modelBuilder.Entity<NotificationMessage>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Recipient).HasMaxLength(320).IsRequired();
            entity.Property(n => n.TemplateKey).HasMaxLength(80).IsRequired();
            entity.Property(n => n.Status).HasMaxLength(32).IsRequired();
            entity.Property(n => n.HtmlBody).HasMaxLength(8000);
            entity.HasIndex(n => new { n.Status, n.NextAttemptAt });
        });
    }
}
