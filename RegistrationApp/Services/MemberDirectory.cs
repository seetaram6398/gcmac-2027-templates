using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gcmac.Registration.Options;
using Microsoft.Extensions.Options;
namespace Gcmac.Registration.Services;

public enum MemberLookupStatus
{
    Found,
    NotFound,
    Invalid,
    Unavailable
}

public sealed record MemberDetails(
    string MemberNo,
    string? Name,
    string? Mobile,
    string? Email,
    string? Designation,
    string? Organisation,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? Country,
    string? PinCode);

public sealed record MemberLookupResult(
    MemberLookupStatus Status,
    MemberDetails? Member,
    string Message)
{
    public static MemberLookupResult Found(MemberDetails member) =>
        new(MemberLookupStatus.Found, member, "Membership verified.");

    public static MemberLookupResult NotFound(string memberNo) =>
        new(MemberLookupStatus.NotFound, null, $"No ICMAI member found for {memberNo}.");

    public static MemberLookupResult Invalid(string message) =>
        new(MemberLookupStatus.Invalid, null, message);

    public static MemberLookupResult Unavailable(string message) =>
        new(MemberLookupStatus.Unavailable, null, message);
}

public interface IMemberDirectory
{
    Task<MemberLookupResult> LookupAsync(string memberNo, CancellationToken cancellationToken = default);
}

public sealed class MemberApiRecord
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("memberNo")] public string? MemberNo { get; set; }
    [JsonPropertyName("memberName")] public string? MemberName { get; set; }
    [JsonPropertyName("mobile")] public string? Mobile { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("Designation")] public string? Designation { get; set; }
    [JsonPropertyName("ORGANISATION_NAME")] public string? Organisation { get; set; }
    [JsonPropertyName("RADDRESS1")] public string? AddressLine1 { get; set; }
    [JsonPropertyName("RADDRESS2")] public string? AddressLine2 { get; set; }
    [JsonPropertyName("RADDRESS3")] public string? AddressLine3 { get; set; }
    [JsonPropertyName("RADDRESS4")] public string? AddressLine4 { get; set; }
    [JsonPropertyName("RCITY")] public string? City { get; set; }
    [JsonPropertyName("RSTATE")] public string? State { get; set; }
    [JsonPropertyName("RCOUNTRY")] public string? Country { get; set; }
    [JsonPropertyName("RPIN")] public string? PinCode { get; set; }

    public MemberDetails ToDetails(string requestedMemberNo) => new(
        MemberNo ?? requestedMemberNo,
        MemberName,
        Mobile,
        Email,
        Designation,
        Organisation,
        JoinAddress(),
        null,
        City,
        State,
        Country,
        PinCode);

    private string? JoinAddress()
    {
        var parts = new[] { AddressLine1, AddressLine2, AddressLine3, AddressLine4 }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(", ", parts);
    }
}

public sealed class MemberDirectory(
    HttpClient client,
    IOptions<MemberApiOptions> options,
    ILogger<MemberDirectory> logger) : IMemberDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _apiKey = options.Value.ApiKey?.Trim() ?? "";

    public async Task<MemberLookupResult> LookupAsync(string memberNo, CancellationToken cancellationToken = default)
    {
        var value = memberNo?.Trim() ?? "";
        if (value.Length == 0 || value.Length > 18 || !value.All(char.IsDigit))
            return MemberLookupResult.Invalid("Enter a numeric ICMAI membership number.");

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"api/member/{Uri.EscapeDataString(value)}");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            if (_apiKey.Length > 0)
                request.Headers.TryAddWithoutValidation("Member-Api-Key", _apiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                logger.LogWarning("ICMAI member directory rejected API credentials (HTTP {StatusCode}).", (int)response.StatusCode);
                return MemberLookupResult.Unavailable("ICMAI member directory authentication failed. Contact the secretariat.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return MemberLookupResult.NotFound(value);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return MemberLookupResult.Invalid("The membership number was rejected by the ICMAI directory.");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ICMAI member directory returned HTTP {StatusCode}.", (int)response.StatusCode);
                return MemberLookupResult.Unavailable("Member directory is temporarily unavailable. Try again later.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var record = JsonSerializer.Deserialize<MemberApiRecord>(json, JsonOptions);
            if (record is null)
                return MemberLookupResult.Unavailable("Invalid response from the ICMAI member directory. Try again later.");

            if (!record.Success)
                return MemberLookupResult.NotFound(
                    string.IsNullOrWhiteSpace(record.Message)
                        ? $"No ICMAI member found for {value}."
                        : record.Message);

            return MemberLookupResult.Found(record.ToDetails(value));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("ICMAI member directory request timed out.");
            return MemberLookupResult.Unavailable("Member directory timed out. Try again later.");
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("ICMAI member directory network error.");
            return MemberLookupResult.Unavailable("Member directory is temporarily unavailable. Try again later.");
        }
    }
}
