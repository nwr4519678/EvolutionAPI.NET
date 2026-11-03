using System.Linq.Expressions;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace EvolutionApi.Tests;

public sealed class ApiKeyValidationTests
{
    [Fact]
    public async Task GlobalKey_IsAccepted()
    {
        var validator = CreateValidator(globalKey: "global-secret");

        var result = await validator.ValidateAsync(new ApiKeyValidationRequest("global-secret", null, "/instance/create"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task InstanceKey_IsAcceptedOnlyForItsInstance()
    {
        var validator = CreateValidator(instance: new Instance
        {
            Name = "clinic",
            ApiKeyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("instance-secret"u8))
        });

        var accepted = await validator.ValidateAsync(new ApiKeyValidationRequest("instance-secret", "clinic", "/message/sendText/clinic"));
        var rejected = await validator.ValidateAsync(new ApiKeyValidationRequest("instance-secret", "other", "/message/sendText/other"));

        Assert.True(accepted.IsValid);
        Assert.False(rejected.IsValid);
    }

    [Fact]
    public async Task MissingKey_IsRejected()
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new ApiKeyValidationRequest(null, null, "/instance/create"));

        Assert.False(result.IsValid);
    }

    private static ApiKeyValidationRequestValidator CreateValidator(string? globalKey = null, Instance? instance = null)
    {
        var values = new Dictionary<string, string?>();
        if (globalKey is not null) values["Evolution:ApiKey"] = globalKey;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new ApiKeyValidationRequestValidator(configuration, new TestInstanceRepository(instance));
    }

    private sealed class TestInstanceRepository(Instance? instance) : IInstanceRepository
    {
        public Task<Instance?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(instance?.Name == name ? instance : null);
        public Task<Instance?> GetByApiKeyHashAsync(string hash, CancellationToken ct = default) =>
            Task.FromResult(instance?.ApiKeyHash == hash ? instance : null);
        public Task<IReadOnlyList<Instance>> GetAllWithDetailsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Instance>>(instance is null ? [] : [instance]);
        public Task<bool> ExistsAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(instance?.Name == name);
        public Task<Instance?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(instance?.Id == id ? instance : null);
        public Task<IReadOnlyList<Instance>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Instance>>(instance is null ? [] : [instance]);
        public Task<Instance?> FindAsync(Expression<Func<Instance, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult(instance is not null && predicate.Compile()(instance) ? instance : null);
        public Task<IReadOnlyList<Instance>> FindAllAsync(Expression<Func<Instance, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Instance>>(instance is not null && predicate.Compile()(instance) ? [instance] : []);
        public Task AddAsync(Instance entity, CancellationToken ct = default) => Task.CompletedTask;
        public void Update(Instance entity) { }
        public void Remove(Instance entity) { }
        public Task<bool> ExistsAsync(Expression<Func<Instance, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult(instance is not null && predicate.Compile()(instance));
    }
}
