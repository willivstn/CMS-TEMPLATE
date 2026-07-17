using CMS.Application.Common.Behaviors;
using CMS.Shared.Results;
using FluentValidation;
using MediatR;

namespace CMS.UnitTests;

public class ValidationBehaviorTests
{
    public sealed record TestCommand(string Name) : IRequest<Result>;

    public sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task Invalid_request_short_circuits_with_failure()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestValidator()]);
        var nextCalled = false;
        RequestHandlerDelegate<Result> next = _ => { nextCalled = true; return Task.FromResult(Result.Success()); };

        var result = await behavior.Handle(new TestCommand(""), next, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Valid_request_invokes_the_handler()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestValidator()]);
        var nextCalled = false;
        RequestHandlerDelegate<Result> next = _ => { nextCalled = true; return Task.FromResult(Result.Success()); };

        var result = await behavior.Handle(new TestCommand("ok"), next, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task No_validators_passes_through()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([]);
        RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Success());

        var result = await behavior.Handle(new TestCommand(""), next, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
