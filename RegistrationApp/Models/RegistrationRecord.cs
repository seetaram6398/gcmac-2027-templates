namespace Gcmac.Registration.Models;

public static class PaymentStates
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Verification = "Under Verification";
}

public sealed class RegistrationRecord
{
    public string Id { get; set; } = "";
    public string MembershipId { get; set; } = "";
    public string AccountEmail { get; set; } = "";
    public DelegateCategory Category { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? Country { get; set; }
    public string? OrganisationName { get; set; }
    public string? OrganisationAddress { get; set; }
    public string? SignatoryName { get; set; }
    public string? Designation { get; set; }
    public string? MembershipNo { get; set; }
    public string? StudentRegistrationNo { get; set; }
    public string? OccupancyType { get; set; }
    public bool GstDetailsRequired { get; set; }
    public string? Gstin { get; set; }
    public string? SponsorMemberNo { get; set; }
    public string? AddedByAccountEmail { get; set; }
    public string PaymentBatchId { get; set; } = "";
    public bool IsAdditionalDelegate { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal BaseFee { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsEarlyBird { get; set; }
    public string PaymentStatus { get; set; } = PaymentStates.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public static RegistrationRecord From(RegistrationInput input, string id, string accountEmail, FeeBreakdown fee) => new()
    {
        Id = id,
        MembershipId = MembershipIds.Build(
            input.SponsorMemberNo ?? (input.Category == DelegateCategory.CmaPractice ? input.MembershipNo : null),
            input.FullName),
        AccountEmail = accountEmail,
        Category = input.Category,
        FullName = input.FullName.Trim(),
        Email = input.Email.Trim(),
        Mobile = input.Mobile.Trim(),
        Gender = input.Gender,
        DateOfBirth = input.DateOfBirth,
        Address = input.Address,
        City = input.City,
        State = input.State,
        PinCode = input.PinCode,
        Country = input.Country,
        OrganisationName = input.OrganisationName,
        OrganisationAddress = input.OrganisationAddress,
        SignatoryName = input.SignatoryName,
        Designation = input.Designation,
        MembershipNo = input.MembershipNo,
        StudentRegistrationNo = input.StudentRegistrationNo,
        OccupancyType = input.OccupancyType,
        GstDetailsRequired = input.GstDetailsRequired,
        Gstin = input.Gstin,
        SponsorMemberNo = input.SponsorMemberNo,
        AddedByAccountEmail = accountEmail,
        Currency = fee.Currency,
        BaseFee = fee.BaseFee,
        GstRate = fee.GstRate,
        GstAmount = fee.GstAmount,
        TotalAmount = fee.TotalAmount,
        IsEarlyBird = fee.IsEarlyBird,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
