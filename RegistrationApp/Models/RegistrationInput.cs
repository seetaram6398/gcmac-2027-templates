using System.Text.RegularExpressions;

namespace Gcmac.Registration.Models;

public sealed class RegistrationInput
{
    public DelegateCategory Category { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string ConfirmEmail { get; set; } = "";
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
    public bool MemberVerified { get; set; }
    public bool RequireMemberVerification { get; set; } = true;
    public bool AgreeTerms { get; set; }

    public List<string> Validate()
    {
        var errors = new List<string>();
        void Required(string? value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) errors.Add($"{label} is required.");
        }

        Required(FullName, "Delegate name");
        Required(Email, "Email");
        Required(Mobile, "Mobile number");
        Required(Address, "Address");
        Required(City, "City");
        Required(PinCode, "PIN / postal code");

        if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors.Add("Enter a valid email address.");

        if (!string.Equals(Email.Trim(), ConfirmEmail?.Trim(), StringComparison.OrdinalIgnoreCase))
            errors.Add("Email and confirm email must match.");

        var mobilePattern = Category == DelegateCategory.Foreign
            ? @"^[+0-9()\-\s]{6,20}$"
            : @"^[0-9]{10}$";
        if (!Regex.IsMatch(Mobile, mobilePattern))
            errors.Add(Category == DelegateCategory.Foreign
                ? "Enter a valid international mobile number."
                : "Mobile number must be 10 digits.");

        if (Category == DelegateCategory.CorporateOthers)
        {
            Required(OrganisationName, "Organisation name");
            Required(OrganisationAddress, "Organisation address");
            Required(SignatoryName, "Signatory name");
            Required(Designation, "Designation");
            Required(OccupancyType, "Occupancy type");
            Required(State, "State");
        }
        else if (Category == DelegateCategory.CmaPractice)
        {
            Required(MembershipNo, "ICMAI membership number");
            Required(Designation, "Designation");
            Required(State, "State");
        }
        else if (Category == DelegateCategory.Student)
        {
            Required(StudentRegistrationNo, "Student registration number");
            Required(DateOfBirth, "Date of birth");
            Required(State, "State");
        }
        else if (Category == DelegateCategory.Foreign)
        {
            Required(Country, "Country");
            Required(DateOfBirth, "Date of birth");
            Required(Gender, "Gender");
        }
        else if (Category == DelegateCategory.Spouse)
        {
            Required(State, "State");
        }

        if (Category == DelegateCategory.CmaPractice && RequireMemberVerification && !MemberVerified)
            errors.Add("Verify your ICMAI membership number before continuing.");

        if (GstDetailsRequired) Required(Gstin, "GSTIN");

        if (!AgreeTerms) errors.Add("Please accept the registration terms.");

        return errors;
    }
}
