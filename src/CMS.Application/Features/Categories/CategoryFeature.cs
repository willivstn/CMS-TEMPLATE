using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Content;
using CMS.Shared.Results;
using CMS.Shared.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CMS.Application.Features.Categories;

// ---- DTOs ----------------------------------------------------------------

public sealed record CategoryListItemDto(Guid Id, string Name, string Slug, string? Description, string? ParentName, int SortOrder, int ContentCount);

public sealed record CategoryEditDto(Guid Id, string Name, string Slug, string? Description, Guid? ParentId, int SortOrder);

public sealed record CategoryParentOptionDto(Guid Id, string Name);

// ---- Queries -------------------------------------------------------------

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryListItemDto>>;

public sealed class GetCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryListItemDto>>
{
    public async Task<IReadOnlyList<CategoryListItemDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryListItemDto(
                c.Id, c.Name, c.Slug, c.Description,
                c.Parent != null ? c.Parent.Name : null,
                c.SortOrder,
                c.ContentItems.Count))
            .ToListAsync(cancellationToken);
}

public sealed record GetCategoryForEditQuery(Guid Id) : IRequest<CategoryEditDto?>;

public sealed class GetCategoryForEditQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoryForEditQuery, CategoryEditDto?>
{
    public async Task<CategoryEditDto?> Handle(GetCategoryForEditQuery request, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryEditDto(c.Id, c.Name, c.Slug, c.Description, c.ParentId, c.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);
}

/// <summary>Parent options, optionally excluding a category (to prevent self-parenting).</summary>
public sealed record GetCategoryParentOptionsQuery(Guid? ExcludeId = null) : IRequest<IReadOnlyList<CategoryParentOptionDto>>;

public sealed class GetCategoryParentOptionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoryParentOptionsQuery, IReadOnlyList<CategoryParentOptionDto>>
{
    public async Task<IReadOnlyList<CategoryParentOptionDto>> Handle(GetCategoryParentOptionsQuery request, CancellationToken cancellationToken) =>
        await db.Categories.AsNoTracking()
            .Where(c => request.ExcludeId == null || c.Id != request.ExcludeId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryParentOptionDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);
}

// ---- Commands ------------------------------------------------------------

public sealed record CreateCategoryCommand(string Name, string? Slug, string? Description, Guid? ParentId, int SortOrder) : IRequest<Result<Guid>>;

public sealed class CreateCategoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = await UniqueSlug(db, request.Slug, request.Name, null, cancellationToken),
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(category.Id);
    }

    internal static async Task<string> UniqueSlug(IApplicationDbContext db, string? provided, string name, Guid? excludeId, CancellationToken ct)
    {
        var baseSlug = Slug.Generate(string.IsNullOrWhiteSpace(provided) ? name : provided);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "category";
        }

        var candidate = baseSlug;
        var suffix = 2;
        while (await db.Categories.AnyAsync(c => c.Slug == candidate && (excludeId == null || c.Id != excludeId), ct))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        return candidate;
    }
}

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Slug, string? Description, Guid? ParentId, int SortOrder) : IRequest<Result>;

public sealed class UpdateCategoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("Category.NotFound", "Category not found."));
        }

        if (request.ParentId == request.Id)
        {
            return Result.Failure(Error.Validation("Category.SelfParent", "A category cannot be its own parent."));
        }

        category.Name = request.Name;
        category.Slug = await CreateCategoryCommandHandler.UniqueSlug(db, request.Slug, request.Name, request.Id, cancellationToken);
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCategoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("Category.NotFound", "Category not found."));
        }

        if (await db.Categories.AnyAsync(c => c.ParentId == request.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("Category.HasChildren", "Remove or reassign child categories first."));
        }

        db.Categories.Remove(category); // soft-deleted; content's CategoryId is left intact
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ---- Validators ----------------------------------------------------------

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
