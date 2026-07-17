using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Communication;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.EmailTemplates;

public sealed record EmailTemplateListDto(Guid Id, string Key, string Subject, bool IsActive);

public sealed record EmailTemplateEditDto(Guid Id, string Key, string Subject, string HtmlBody, string? TextBody, string? Description, string? AvailableTokens, bool IsActive);

public sealed record GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplateListDto>>;

public sealed class GetEmailTemplatesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateListDto>>
{
    public async Task<IReadOnlyList<EmailTemplateListDto>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken) =>
        await db.EmailTemplates.AsNoTracking()
            .OrderBy(t => t.Key)
            .Select(t => new EmailTemplateListDto(t.Id, t.Key, t.Subject, t.IsActive))
            .ToListAsync(cancellationToken);
}

public sealed record GetEmailTemplateForEditQuery(Guid Id) : IRequest<EmailTemplateEditDto?>;

public sealed class GetEmailTemplateForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmailTemplateForEditQuery, EmailTemplateEditDto?>
{
    public async Task<EmailTemplateEditDto?> Handle(GetEmailTemplateForEditQuery request, CancellationToken cancellationToken) =>
        await db.EmailTemplates.AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new EmailTemplateEditDto(t.Id, t.Key, t.Subject, t.HtmlBody, t.TextBody, t.Description, t.AvailableTokens, t.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed record CreateEmailTemplateCommand(string Key, string Subject, string HtmlBody, string? TextBody, string? Description, bool IsActive) : IRequest<Result<Guid>>;

public sealed class CreateEmailTemplateCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateEmailTemplateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        if (await db.EmailTemplates.AnyAsync(t => t.Key == request.Key, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("EmailTemplate.Exists", "A template with that key already exists."));
        }

        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Key = request.Key.Trim(),
            Subject = request.Subject,
            HtmlBody = request.HtmlBody,
            TextBody = request.TextBody,
            Description = request.Description,
            IsActive = request.IsActive,
        };

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(template.Id);
    }
}

public sealed record UpdateEmailTemplateCommand(Guid Id, string Subject, string HtmlBody, string? TextBody, string? Description, bool IsActive) : IRequest<Result>;

public sealed class UpdateEmailTemplateCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateEmailTemplateCommand, Result>
{
    public async Task<Result> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure(Error.NotFound("EmailTemplate.NotFound", "Template not found."));
        }

        template.Subject = request.Subject;
        template.HtmlBody = request.HtmlBody;
        template.TextBody = request.TextBody;
        template.Description = request.Description;
        template.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteEmailTemplateCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteEmailTemplateCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteEmailTemplateCommand, Result>
{
    public async Task<Result> Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (template is null)
        {
            return Result.Failure(Error.NotFound("EmailTemplate.NotFound", "Template not found."));
        }

        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateEmailTemplateCommandValidator : AbstractValidator<CreateEmailTemplateCommand>
{
    public CreateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(256);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }
}

public sealed class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(256);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }
}
