namespace NetMX.Identity.Core.Security;

/// <summary>
/// Standard claim types for the identity system.
/// </summary>
public static class NetMXClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "name";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string PhoneNumber = "phone_number";
    public const string PhoneNumberVerified = "phone_number_verified";
    public const string Role = "role";
    public const string TenantId = "tenant_id";
    public const string TenantName = "tenant_name";
    public const string Permission = "permission";
    public const string GivenName = "given_name";
    public const string FamilyName = "family_name";
}
