namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Symmetric protection for sensitive values at rest (e.g. encrypted settings).
/// Implemented in the Web layer over ASP.NET Core Data Protection.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);

    /// <summary>Decrypts a value; returns null if the input cannot be unprotected.</summary>
    string? Decrypt(string ciphertext);
}
