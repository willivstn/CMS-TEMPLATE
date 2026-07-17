using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Theming;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Theming;

// ---- DTOs ----------------------------------------------------------------

public sealed record ThemeDto(Guid Id, string Key, string Name, bool IsDark, bool IsDefault, bool IsEnabled, string? Variables, int SortOrder);

public sealed record ThemeOptionDto(string Key, string Name, bool IsDark);

/// <summary>The applied theme's CSS custom-property overrides and base mode.</summary>
public sealed record ActiveThemeDto(string Key, bool IsDark, string? Variables);

// ---- Queries -------------------------------------------------------------

public sealed record GetThemesQuery : IRequest<IReadOnlyList<ThemeDto>>;

public sealed class GetThemesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetThemesQuery, IReadOnlyList<ThemeDto>>
{
    public async Task<IReadOnlyList<ThemeDto>> Handle(GetThemesQuery request, CancellationToken cancellationToken) =>
        await db.Themes.AsNoTracking()
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new ThemeDto(t.Id, t.Key, t.Name, t.IsDark, t.IsDefault, t.IsEnabled, t.Variables, t.SortOrder))
            .ToListAsync(cancellationToken);
}

public sealed record GetEnabledThemesQuery : IRequest<IReadOnlyList<ThemeOptionDto>>;

public sealed class GetEnabledThemesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEnabledThemesQuery, IReadOnlyList<ThemeOptionDto>>
{
    public async Task<IReadOnlyList<ThemeOptionDto>> Handle(GetEnabledThemesQuery request, CancellationToken cancellationToken) =>
        await db.Themes.AsNoTracking()
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new ThemeOptionDto(t.Key, t.Name, t.IsDark))
            .ToListAsync(cancellationToken);
}

public sealed record GetThemeByKeyQuery(string Key) : IRequest<ActiveThemeDto?>;

public sealed class GetThemeByKeyQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetThemeByKeyQuery, ActiveThemeDto?>
{
    public async Task<ActiveThemeDto?> Handle(GetThemeByKeyQuery request, CancellationToken cancellationToken) =>
        await db.Themes.AsNoTracking()
            .Where(t => t.Key == request.Key && t.IsEnabled)
            .Select(t => new ActiveThemeDto(t.Key, t.IsDark, t.Variables))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed record GetThemeForEditQuery(Guid Id) : IRequest<ThemeDto?>;

public sealed class GetThemeForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetThemeForEditQuery, ThemeDto?>
{
    public async Task<ThemeDto?> Handle(GetThemeForEditQuery request, CancellationToken cancellationToken) =>
        await db.Themes.AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new ThemeDto(t.Id, t.Key, t.Name, t.IsDark, t.IsDefault, t.IsEnabled, t.Variables, t.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);
}

// ---- Commands ------------------------------------------------------------

public sealed record CreateThemeCommand(string Key, string Name, bool IsDark, bool IsEnabled, string? Variables, int SortOrder) : IRequest<Result<Guid>>;

public sealed class CreateThemeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateThemeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateThemeCommand request, CancellationToken cancellationToken)
    {
        var key = request.Key.Trim().ToLowerInvariant();
        if (await db.Themes.AnyAsync(t => t.Key == key, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Theme.Exists", "A theme with that key already exists."));
        }

        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = request.Name.Trim(),
            IsDark = request.IsDark,
            IsEnabled = request.IsEnabled,
            Variables = request.Variables,
            SortOrder = request.SortOrder,
        };

        db.Themes.Add(theme);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(theme.Id);
    }
}

public sealed record UpdateThemeCommand(Guid Id, string Name, bool IsDark, bool IsEnabled, string? Variables, int SortOrder) : IRequest<Result>;

public sealed class UpdateThemeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateThemeCommand, Result>
{
    public async Task<Result> Handle(UpdateThemeCommand request, CancellationToken cancellationToken)
    {
        var theme = await db.Themes.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (theme is null)
        {
            return Result.Failure(Error.NotFound("Theme.NotFound", "Theme not found."));
        }

        theme.Name = request.Name;
        theme.IsDark = request.IsDark;
        theme.IsEnabled = request.IsEnabled;
        theme.Variables = request.Variables;
        theme.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteThemeCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteThemeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteThemeCommand, Result>
{
    public async Task<Result> Handle(DeleteThemeCommand request, CancellationToken cancellationToken)
    {
        var theme = await db.Themes.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (theme is null)
        {
            return Result.Failure(Error.NotFound("Theme.NotFound", "Theme not found."));
        }

        if (theme.IsDefault)
        {
            return Result.Failure(Error.Conflict("Theme.Default", "The default theme cannot be deleted."));
        }

        db.Themes.Remove(theme);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateThemeCommandValidator : AbstractValidator<CreateThemeCommand>
{
    public CreateThemeCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Variables).MaximumLength(4000);
    }
}
