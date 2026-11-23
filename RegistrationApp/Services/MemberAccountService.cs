using Gcmac.Registration.Data;
using Gcmac.Registration.Models;

namespace Gcmac.Registration.Services;

public sealed record MemberLoadResult(MemberAccount? Member, string Message, bool FromApi);

public interface IMemberAccountService
{
    Task<MemberLoadResult> LoadAsync(string memberNo, CancellationToken cancellationToken = default);
    Task<MemberAccount?> GetStoredAsync(string memberNo, CancellationToken cancellationToken = default);
    Task<MemberAccount> MarkVerifiedAsync(MemberAccount member, bool emailVerified, bool mobileVerified, CancellationToken cancellationToken = default);
}

public sealed class MemberAccountService(IMemberDirectory directory, RegistrationStore store) : IMemberAccountService
{
    public async Task<MemberLoadResult> LoadAsync(string memberNo, CancellationToken cancellationToken = default)
    {
        var result = await directory.LookupAsync(memberNo, cancellationToken);
        if (result.Status == MemberLookupStatus.Found && result.Member is not null)
        {
            var existing = await store.FindMemberAccountAsync(memberNo, cancellationToken);
            var member = Map(result.Member, existing);
            await store.SaveMemberAccountAsync(member, cancellationToken);
            return new MemberLoadResult(member, "", true);
        }

        var stored = await store.FindMemberAccountAsync(memberNo, cancellationToken);
        if (stored is not null)
            return new MemberLoadResult(stored, "Using your saved member profile.", false);

        return new MemberLoadResult(null, result.Message, false);
    }

    public Task<MemberAccount?> GetStoredAsync(string memberNo, CancellationToken cancellationToken = default) =>
        store.FindMemberAccountAsync(memberNo, cancellationToken);

    public async Task<MemberAccount> MarkVerifiedAsync(
        MemberAccount member,
        bool emailVerified,
        bool mobileVerified,
        CancellationToken cancellationToken = default)
    {
        member.EmailVerified = emailVerified;
        member.MobileVerified = mobileVerified;
        await store.SaveMemberAccountAsync(member, cancellationToken);
        return member;
    }

    private static MemberAccount Map(MemberDetails details, MemberAccount? existing)
    {
        var now = DateTimeOffset.UtcNow;
        return new MemberAccount
        {
            MemberNo = details.MemberNo,
            Name = details.Name ?? existing?.Name ?? "",
            Mobile = details.Mobile ?? existing?.Mobile ?? "",
            Email = details.Email ?? existing?.Email ?? "",
            Designation = details.Designation ?? existing?.Designation,
            Organisation = details.Organisation ?? existing?.Organisation,
            AddressLine1 = details.AddressLine1 ?? existing?.AddressLine1,
            AddressLine2 = details.AddressLine2 ?? existing?.AddressLine2,
            City = details.City ?? existing?.City,
            State = details.State ?? existing?.State,
            Country = details.Country ?? existing?.Country,
            PinCode = details.PinCode ?? existing?.PinCode,
            EmailVerified = existing?.EmailVerified ?? false,
            MobileVerified = existing?.MobileVerified ?? false,
            CreatedAt = existing is not null && existing.CreatedAt != default ? existing.CreatedAt : now,
            UpdatedAt = now
        };
    }
}
