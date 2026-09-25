using FluentValidation;
using TestJob.Api.Models;
using TestJob.Api.Validation;

namespace TestJob.Api.Tests;

public sealed class ProcessPageRequestValidatorTests
{
    [Fact]
    public async Task ValidateAsync_MissingFields_ReturnsOneErrorPerField()
    {
        var validator = new ProcessPageRequestValidator();

        var result = await validator.ValidateAsync(new ProcessPageRequest());

        Assert.False(result.IsValid);
        Assert.Equal(6, result.Errors.Count);
    }
}
