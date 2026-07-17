using System.Security.Cryptography;
using System.Text;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Configuration;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.ApiKeys;

public sealed record ApiKeyListDto(Guid Id, string Name, string Prefix, string? Scopes, DateTimeOffset? ExpiresOnUtc, DateTimeOffset? LastUsedUtc, bool IsActive);

/// <summary>The one-time plaintext key returned when a key is created.</summary>
public sealed record CreatedApiKey(Guid Id, string PlainKey);

public sealed record GetApiKeysQuery : IRequest<IReadOnlyList<ApiKeyListDto>>;

public sealed class GetApiKeysQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyListDto>>
{
    public async Task<IReadOnlyList<ApiKeyListDto>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken) =>
        await db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedOnUtc)
            .Select(k => new ApiKeyListDto(k.Id, k.Name, k.Prefix, k.Scopes, k.ExpiresOnUtc, k.LastUsedUtc, k.IsActive))
            .ToListAsync(cancellationToken);
}

/// <summary>
/// Creates an API key. The full key is returned once, in plaintext; only a hash
/// and a short lookup prefix are stored, so the key cannot be recovered later.
/// </summary>
public sealed record CreateApiKeyCommand(string Name, string? Scopes, DateTimeOffset? ExpiresOnUtc) : IRequest<Result<CreatedApiKey>>;

public sealed class CreateApiKeyCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateApiKeyCommand, Result<CreatedApiKey>>
{
    public async Task<Result<CreatedApiKey>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var prefix = "cms_" + ToBase64Url(RandomNumberGenerator.GetBytes(6));
        var secret = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        var fullKey = $"{prefix}.{secret}";

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Prefix = prefix,
            KeyHash = Hash(fullKey),
            Scopes = request.Scopes,
            ExpiresOnUtc = request.ExpiresOnUtc,
            IsActive = true,
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CreatedApiKey(apiKey.Id, fullKey));
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed record RevokeApiKeyCommand(Guid Id) : IRequest<Result>;

public sealed class RevokeApiKeyCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RevokeApiKeyCommand, Result>
{
    public async Task<Result> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == request.Id, cancellationToken);
        if (key is null)
        {
            return Result.Failure(Error.NotFound("ApiKey.NotFound", "API key not found."));
        }

        key.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
}
