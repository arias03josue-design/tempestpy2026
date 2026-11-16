using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tempest.Application;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Security;
using Tempest.Application.Users;
using Tempest.Application.Users.Commands;
using Tempest.Infrastructure;

namespace Tempest.Tests.Integration;

/// <summary>
/// Real Application + Infrastructure stack (dispatcher, behaviors, EF Core migrations, SQLite file)
/// in an isolated temp folder. Only the presentation-layer identity source is faked.
/// </summary>
public sealed class TestApp : IAsyncDisposable
{
    private readonly ServiceProvider _services;
    private readonly FakeCurrentUserProvider _currentUser;

    private TestApp(string root, ServiceProvider services, FakeCurrentUserProvider currentUser)
    {
        Root = root;
        _services = services;
        _currentUser = currentUser;
    }

    public string Root { get; }

    public string UploadsPath => Path.Combine(Root, "uploads");

    public static async Task<TestApp> CreateAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "tempest-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Data Source=data/test.db",
                ["Storage:UploadsPath"] = "uploads",
                ["Seed:AdminUsername"] = "admin",
                ["Seed:AdminPassword"] = "admin",
                ["Seed:SampleProducts"] = "true",
            })
            .Build();

        var currentUser = new FakeCurrentUserProvider();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration, root)
            .AddSingleton<ICurrentUserProvider>(currentUser)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await services.InitializeDatabaseAsync();
        return new TestApp(root, services, currentUser);
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>Logs in with the given credentials and makes that user the caller of the next requests.</summary>
    public async Task<AuthenticatedUserDto> SignInAsync(string username = "admin", string password = "admin")
    {
        var result = await SendAsync(new LoginCommand(username, password));
        Assert.True(result.IsSuccess, result.Error?.Message);
        _currentUser.User = new CurrentUser(result.Value.Id, result.Value.Username);
        return result.Value;
    }

    public void SignOut() => _currentUser.User = CurrentUser.Anonymous;

    public Task<string> BackupAsync(string destinationPath) => _services.BackupDatabaseAsync(destinationPath);

    public async Task<bool> IsSessionValidAsync(Guid userId, string securityStamp)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUserSessionValidator>().IsValidAsync(userId, securityStamp);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: the OS temp folder is cleaned up eventually.
        }
    }

    private sealed class FakeCurrentUserProvider : ICurrentUserProvider
    {
        public CurrentUser User { get; set; } = CurrentUser.Anonymous;

        public ValueTask<CurrentUser> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(User);
    }
}
