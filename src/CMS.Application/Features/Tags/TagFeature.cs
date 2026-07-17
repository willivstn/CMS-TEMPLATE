using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Content;
using CMS.Shared.Results;
using CMS.Shared.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Tags;

public sealed record TagListItemDto(Guid Id, string Name, string Slug, int UsageCount);

public sealed record GetTagsQuery : IRequest<IReadOnlyList<TagListItemDto>>;

public sealed class GetTagsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTagsQuery, IReadOnlyList<TagListItemDto>>
{
    public async Task<IReadOnlyList<TagListItemDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken) =>
        await db.Tags.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagListItemDto(t.Id, t.Name, t.Slug, t.ContentTags.Count))
            .ToListAsync(cancellationToken);
}

public sealed record CreateTagCommand(string Name) : IRequest<Result<Guid>>;

public sealed class CreateTagCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTagCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var slug = Slug.Generate(request.Name);
        if (await db.Tags.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Tag.Exists", "A tag with that name already exists."));
        }

        var tag = new Tag { Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = slug };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(tag.Id);
    }
}

public sealed record DeleteTagCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteTagCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteTagCommand, Result>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tag is null)
        {
            return Result.Failure(Error.NotFound("Tag.NotFound", "Tag not found."));
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(96);
}
