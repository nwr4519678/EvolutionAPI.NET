using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Common.Pagination;

namespace EvolutionApi.Tests;

public sealed class PaginationAndResultTests
{
    [Theory]
    [InlineData(-1, -5, 1, 20)]
    [InlineData(3, 500, 3, 100)]
    [InlineData(2, 25, 2, 25)]
    public void PaginationParams_NormalizesInvalidAndOversizedValues(int page, int size, int expectedPage, int expectedSize)
    {
        var result = new PaginationParams(page, size);

        Assert.Equal(expectedPage, result.PageNumber);
        Assert.Equal(expectedSize, result.PageSize);
    }

    [Fact]
    public void Result_Map_TransformsOnlySuccessfulValues()
    {
        Result<int> result = 21;

        var mapped = result.Map(value => value * 2);

        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void FailedResult_PreservesErrorAndRejectsValueAccess()
    {
        var error = AppError.Validation("Invalid recipient");
        Result<string> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
