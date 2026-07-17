using CMS.Shared.Results;

namespace CMS.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_is_successful_with_no_error()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var error = Error.NotFound("X.NotFound", "Missing");
        var result = Result.Failure(error);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        Result<int> result = 42;
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Accessing_value_of_failure_throws()
    {
        Result<int> result = Error.Failure("E", "boom");
        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Match_projects_both_branches()
    {
        Result<int> ok = 10;
        Result<int> fail = Error.Validation("E", "bad");

        Assert.Equal("ok:10", ok.Match(v => $"ok:{v}", e => $"err:{e.Code}"));
        Assert.Equal("err:E", fail.Match(v => $"ok:{v}", e => $"err:{e.Code}"));
    }

    [Fact]
    public void Cannot_create_successful_result_with_error()
        => Assert.Throws<InvalidOperationException>(() => new TestResult(true, Error.Failure("E", "x")));

    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
