using CMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace CMS.Web.Security;

/// <summary>
/// <see cref="IEncryptionService"/> over ASP.NET Core Data Protection. Used to
/// protect encrypted settings at rest. Keys are managed by the Data Protection
/// stack (persist them to a shared store in multi-instance deployments).
/// </summary>
public sealed class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionService(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("CMS.Settings.v1");

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string? Decrypt(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}
