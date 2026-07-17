using FluentValidation;

namespace CMS.Application.Features.Content;

public sealed class CreateContentCommandValidator : AbstractValidator<CreateContentCommand>
{
    public CreateContentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).MaximumLength(256);
        RuleFor(x => x.MetaTitle).MaximumLength(256);
        RuleFor(x => x.MetaDescription).MaximumLength(512);
    }
}

public sealed class UpdateContentCommandValidator : AbstractValidator<UpdateContentCommand>
{
    public UpdateContentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).MaximumLength(256);
        RuleFor(x => x.MetaTitle).MaximumLength(256);
        RuleFor(x => x.MetaDescription).MaximumLength(512);
    }
}
