using System.Text.Json;
using Gcmac.Registration.Models;
using Microsoft.EntityFrameworkCore;

namespace Gcmac.Registration.Data;

public sealed class RegistrationStore
{
    private sealed class AppState
    {
        public List<Account> Accounts { get; set; } = [];
        public List<MemberAccount> MemberAccounts { get; set; } = [];
        public List<RegistrationRecord> Registrations { get; set; } = [];
        public List<PaymentTransaction> PaymentTransactions { get; set; } = [];
        public List<NotificationMessage> Notifications { get; set; } = [];
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IDbContextFactory<AppDbContext>? _factory;
    private readonly string _filePath;

    public RegistrationStore(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IDbContextFactory<AppDbContext> factory)
    {
        _factory = string.IsNullOrWhiteSpace(configuration.GetConnectionString("SqlServer"))
            ? null
            : factory;
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "registrations.json");
    }

    private AppState Read()
    {
        if (!File.Exists(_filePath)) return new AppState();
        var json = File.ReadAllText(_filePath);
        return string.IsNullOrWhiteSpace(json)
            ? new AppState()
            : JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
    }

    private void Write(AppState state) =>
        File.WriteAllText(_filePath, JsonSerializer.Serialize(state, Options));

    private async Task<T> FileReadAsync<T>(Func<AppState, T> operation)
    {
        await _gate.WaitAsync();
        try
        {
            return operation(Read());
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T> FileWriteAsync<T>(Func<AppState, T> operation)
    {
        await _gate.WaitAsync();
        try
        {
            var state = Read();
            var result = operation(state);
            Write(state);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AccountExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.Accounts.Any(a =>
                string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Accounts.AnyAsync(a => a.Email == email, cancellationToken);
    }

    public async Task AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                state.Accounts.Add(account);
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Account?> FindAccountAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.Accounts.FirstOrDefault(a =>
                string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);
    }

    public async Task<MemberAccount?> FindMemberAccountAsync(string memberNo, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.MemberAccounts.FirstOrDefault(m =>
                string.Equals(m.MemberNo, memberNo, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.MemberAccounts.FirstOrDefaultAsync(m => m.MemberNo == memberNo, cancellationToken);
    }

    public async Task<MemberAccount?> FindMemberAccountByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.MemberAccounts.FirstOrDefault(m =>
                string.Equals(m.Email, email, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.MemberAccounts.FirstOrDefaultAsync(m => m.Email == email, cancellationToken);
    }

    public async Task SaveMemberAccountAsync(MemberAccount member, CancellationToken cancellationToken = default)
    {
        member.UpdatedAt = DateTimeOffset.UtcNow;
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                var index = state.MemberAccounts.FindIndex(m =>
                    string.Equals(m.MemberNo, member.MemberNo, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) state.MemberAccounts[index] = member;
                else state.MemberAccounts.Add(member);
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var exists = await db.MemberAccounts.AnyAsync(m => m.MemberNo == member.MemberNo, cancellationToken);
        if (exists)
        {
            member.UpdatedAt = DateTimeOffset.UtcNow;
            db.MemberAccounts.Update(member);
        }
        else
        {
            member.CreatedAt = member.CreatedAt == default ? DateTimeOffset.UtcNow : member.CreatedAt;
            db.MemberAccounts.Add(member);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RegistrationRecord> AddRegistrationAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileWriteAsync(state =>
            {
                while (state.Registrations.Any(r => r.Id == record.Id))
                    record.Id = GenerateId();
                state.Registrations.Add(record);
                return record;
            });

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        while (await db.Registrations.AnyAsync(r => r.Id == record.Id, cancellationToken))
            record.Id = GenerateId();
        db.Registrations.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task SaveRegistrationAsync(RegistrationRecord record, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                var index = state.Registrations.FindIndex(r => r.Id == record.Id);
                if (index >= 0) state.Registrations[index] = record;
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Registrations.Update(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RegistrationRecord?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.Registrations.FirstOrDefault(r =>
                string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Registrations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => (IReadOnlyList<RegistrationRecord>)state.Registrations
                .OrderByDescending(r => r.CreatedAt).ToList());

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Registrations.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationRecord>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => (IReadOnlyList<RegistrationRecord>)state.Registrations
                .Where(r => string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.CreatedAt).ToList());

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Registrations.AsNoTracking()
            .Where(r => r.Email == email)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationRecord>> GetBySponsorMemberNoAsync(string memberNo, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => (IReadOnlyList<RegistrationRecord>)state.Registrations
                .Where(r => string.Equals(r.SponsorMemberNo, memberNo, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.CreatedAt).ToList());

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Registrations.AsNoTracking()
            .Where(r => r.SponsorMemberNo == memberNo)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<int> CountBySponsorMemberNoAsync(string memberNo, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.Registrations.Count(r =>
                string.Equals(r.SponsorMemberNo, memberNo, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Registrations.CountAsync(r => r.SponsorMemberNo == memberNo, cancellationToken);
    }

    public async Task AddPaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                state.PaymentTransactions.Add(transaction);
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentTransaction?> FindPaymentTransactionAsync(string merchantTxnRefNo, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => state.PaymentTransactions.FirstOrDefault(t =>
                string.Equals(t.MerchantTxnRefNo, merchantTxnRefNo, StringComparison.OrdinalIgnoreCase)));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.PaymentTransactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.MerchantTxnRefNo == merchantTxnRefNo, cancellationToken);
    }

    public async Task SavePaymentTransactionAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                var index = state.PaymentTransactions.FindIndex(t =>
                    t.MerchantTxnRefNo == transaction.MerchantTxnRefNo);
                if (index >= 0) state.PaymentTransactions[index] = transaction;
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.PaymentTransactions.Update(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> EnqueueNotificationAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        message.CreatedAt = now;
        message.NextAttemptAt = now;
        message.Status = NotificationStatus.Pending;
        message.Attempts = 0;

        if (_factory is null)
            return await FileWriteAsync(state =>
            {
                var nextId = state.Notifications.Count == 0 ? 1 : state.Notifications.Max(n => n.Id) + 1;
                message.Id = nextId;
                state.Notifications.Add(message);
                return nextId;
            });

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Notifications.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return message.Id;
    }

    public async Task<IReadOnlyList<NotificationMessage>> GetPendingNotificationsAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            return await FileReadAsync(state => (IReadOnlyList<NotificationMessage>)state.Notifications
                .Where(n => n.Status == NotificationStatus.Pending && n.NextAttemptAt <= now)
                .OrderBy(n => n.Id).Take(take).ToList());

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Notifications.AsNoTracking()
            .Where(n => n.Status == NotificationStatus.Pending && n.NextAttemptAt <= now)
            .OrderBy(n => n.Id).Take(take).ToListAsync(cancellationToken);
    }

    public async Task SaveNotificationAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (_factory is null)
        {
            await FileWriteAsync(state =>
            {
                var index = state.Notifications.FindIndex(n => n.Id == message.Id);
                if (index >= 0) state.Notifications[index] = message;
                return true;
            });
            return;
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Notifications.Update(message);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static string GenerateId() =>
        $"GCMAC27-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public static string GenerateMerchantRef() =>
        $"GCMAC27{DateTime.UtcNow:ddMMyyyyHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
