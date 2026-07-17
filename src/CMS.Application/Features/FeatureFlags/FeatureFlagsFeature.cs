using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Configuration;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.FeatureFlags;

public sealed record FeatureFlagDto(Guid Id, string Name, bool IsEnabled, string? Description, string? Environment);

public sealed record GetFeatureFlagsQuery : IRequest<IReadOnlyList<FeatureFlagDto>>;

public sealed class GetFeatureFlagsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFeatureFlagsQuery, IReadOnlyList<FeatureFlagDto>>
{
    public async Task<IReadOnlyList<FeatureFlagDto>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken) =>
        await db.FeatureFlags.AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FeatureFlagDto(f.Id, f.Name, f.IsEnabled, f.Description, f.Environment))
            .ToListAsync(cancellationToken);
}

public sealed record CreateFeatureFlagCommand(string Name, string? Description, string? Environment, bool IsEnabled) : IRequest<Result<Guid>>;

public sealed class CreateFeatureFlagCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<CreateFeatureFlagCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        if (await db.FeatureFlags.AnyAsync(f => f.Name == request.Name, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Feature.Exists", "A flag with that name already exists."));
        }

        var flag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Environment = string.IsNullOrWhiteSpace(request.Environment) ? null : request.Environment.Trim(),
            IsEnabled = request.IsEnabled,
        };

        db.FeatureFlags.Add(flag);
        await db.SaveChangesAsync(cancellationToken);
        cache.Remove($"feature:{flag.Name}");
        return Result.Success(flag.Id);
    }
}

public sealed record ToggleFeatureFlagCommand(Guid Id, bool IsEnabled) : IRequest<Result>;

public sealed class ToggleFeatureFlagCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<ToggleFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (flag is null)
        {
            return Result.Failure(Error.NotFound("Feature.NotFound", "Flag not found."));
        }

        flag.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync(cancellationToken);
        cache.Remove($"feature:{flag.Name}");
        return Result.Success();
    }
}

public sealed record DeleteFeatureFlagCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteFeatureFlagCommandHandler(IApplicationDbContext db, ICacheService cache)
    : IRequestHandler<DeleteFeatureFlagCommand, Result>
{
    public async Task<Result> Handle(DeleteFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
        if (flag is null)
        {
            return Result.Failure(Error.NotFound("Feature.NotFound", "Flag not found."));
        }

        db.FeatureFlags.Remove(flag);
        await db.SaveChangesAsync(cancellationToken);
        cache.Remove($"feature:{flag.Name}");
        return Result.Success();
    }
}

public sealed class CreateFeatureFlagCommandValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
