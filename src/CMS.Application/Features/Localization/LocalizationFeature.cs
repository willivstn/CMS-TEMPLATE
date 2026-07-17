using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Extensions;
using CMS.Domain.Entities.Localization;
using CMS.Shared.Models;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Localization;

// ---- DTOs ----------------------------------------------------------------

public sealed record LanguageDto(Guid Id, string Code, string Name, string? NativeName, bool IsRtl, bool IsDefault, bool IsEnabled, int SortOrder);

public sealed record LanguageOptionDto(string Code, string Name);

public sealed record TranslationDto(Guid Id, string Key, string LanguageCode, string Value, string? Group);

// ---- Language queries ----------------------------------------------------

public sealed record GetLanguagesQuery : IRequest<IReadOnlyList<LanguageDto>>;

public sealed class GetLanguagesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLanguagesQuery, IReadOnlyList<LanguageDto>>
{
    public async Task<IReadOnlyList<LanguageDto>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken) =>
        await db.Languages.AsNoTracking()
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .Select(l => new LanguageDto(l.Id, l.Code, l.Name, l.NativeName, l.IsRtl, l.IsDefault, l.IsEnabled, l.SortOrder))
            .ToListAsync(cancellationToken);
}

public sealed record GetLanguageOptionsQuery : IRequest<IReadOnlyList<LanguageOptionDto>>;

public sealed class GetLanguageOptionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLanguageOptionsQuery, IReadOnlyList<LanguageOptionDto>>
{
    public async Task<IReadOnlyList<LanguageOptionDto>> Handle(GetLanguageOptionsQuery request, CancellationToken cancellationToken) =>
        await db.Languages.AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LanguageOptionDto(l.Code, l.Name))
            .ToListAsync(cancellationToken);
}

// ---- Language commands ---------------------------------------------------

public sealed record CreateLanguageCommand(string Code, string Name, string? NativeName, bool IsRtl, bool IsEnabled, int SortOrder) : IRequest<Result<Guid>>;

public sealed class CreateLanguageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateLanguageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        if (await db.Languages.AnyAsync(l => l.Code == request.Code, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Language.Exists", "A language with that code already exists."));
        }

        var language = new Language
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            NativeName = request.NativeName,
            IsRtl = request.IsRtl,
            IsEnabled = request.IsEnabled,
            SortOrder = request.SortOrder,
        };

        db.Languages.Add(language);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(language.Id);
    }
}

public sealed record UpdateLanguageCommand(Guid Id, string Name, string? NativeName, bool IsRtl, bool IsEnabled, int SortOrder) : IRequest<Result>;

public sealed class UpdateLanguageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateLanguageCommand, Result>
{
    public async Task<Result> Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await db.Languages.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (language is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", "Language not found."));
        }

        language.Name = request.Name;
        language.NativeName = request.NativeName;
        language.IsRtl = request.IsRtl;
        language.IsEnabled = request.IsEnabled;
        language.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SetDefaultLanguageCommand(Guid Id) : IRequest<Result>;

public sealed class SetDefaultLanguageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetDefaultLanguageCommand, Result>
{
    public async Task<Result> Handle(SetDefaultLanguageCommand request, CancellationToken cancellationToken)
    {
        var languages = await db.Languages.ToListAsync(cancellationToken);
        var target = languages.FirstOrDefault(l => l.Id == request.Id);
        if (target is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", "Language not found."));
        }

        foreach (var language in languages)
        {
            language.IsDefault = language.Id == request.Id;
        }

        target.IsEnabled = true;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteLanguageCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteLanguageCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteLanguageCommand, Result>
{
    public async Task<Result> Handle(DeleteLanguageCommand request, CancellationToken cancellationToken)
    {
        var language = await db.Languages.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        if (language is null)
        {
            return Result.Failure(Error.NotFound("Language.NotFound", "Language not found."));
        }

        if (language.IsDefault)
        {
            return Result.Failure(Error.Conflict("Language.Default", "Set another language as default first."));
        }

        db.Languages.Remove(language);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Translation queries/commands ----------------------------------------

public sealed record GetTranslationsQuery(string? LanguageCode, string? Group, string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<TranslationDto>>;

public sealed class GetTranslationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTranslationsQuery, PagedResult<TranslationDto>>
{
    public async Task<PagedResult<TranslationDto>> Handle(GetTranslationsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Translations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            query = query.Where(t => t.LanguageCode == request.LanguageCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            query = query.Where(t => t.Group == request.Group);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(t => t.Key.Contains(term) || t.Value.Contains(term));
        }

        return await query
            .OrderBy(t => t.Key).ThenBy(t => t.LanguageCode)
            .Select(t => new TranslationDto(t.Id, t.Key, t.LanguageCode, t.Value, t.Group))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }
}

public sealed record CreateTranslationCommand(string Key, string LanguageCode, string Value, string? Group) : IRequest<Result<Guid>>;

public sealed class CreateTranslationCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTranslationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTranslationCommand request, CancellationToken cancellationToken)
    {
        if (await db.Translations.AnyAsync(t => t.Key == request.Key && t.LanguageCode == request.LanguageCode, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Translation.Exists", "That key already exists for this language."));
        }

        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            Key = request.Key.Trim(),
            LanguageCode = request.LanguageCode,
            Value = request.Value,
            Group = request.Group,
        };

        db.Translations.Add(translation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(translation.Id);
    }
}

public sealed record UpdateTranslationCommand(Guid Id, string Value, string? Group) : IRequest<Result>;

public sealed class UpdateTranslationCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateTranslationCommand, Result>
{
    public async Task<Result> Handle(UpdateTranslationCommand request, CancellationToken cancellationToken)
    {
        var translation = await db.Translations.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (translation is null)
        {
            return Result.Failure(Error.NotFound("Translation.NotFound", "Translation not found."));
        }

        translation.Value = request.Value;
        translation.Group = request.Group;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteTranslationCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteTranslationCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteTranslationCommand, Result>
{
    public async Task<Result> Handle(DeleteTranslationCommand request, CancellationToken cancellationToken)
    {
        var translation = await db.Translations.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (translation is null)
        {
            return Result.Failure(Error.NotFound("Translation.NotFound", "Translation not found."));
        }

        db.Translations.Remove(translation);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Validators ----------------------------------------------------------

public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}

public sealed class CreateTranslationCommandValidator : AbstractValidator<CreateTranslationCommand>
{
    public CreateTranslationCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LanguageCode).NotEmpty();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2048);
    }
}
