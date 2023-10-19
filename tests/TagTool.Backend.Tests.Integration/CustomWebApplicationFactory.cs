using System.Data.Common;
using Grpc.Net.Client;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TagTool.Backend.DbContext;
using TagTool.Backend.Tests.Integration.Helpers;

namespace TagTool.Backend.Tests.Integration;

public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

[UsedImplicitly]
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public event LogMessage? LoggedMessage;

    private GrpcChannel? _channel;

    public GrpcChannel Channel => _channel ??= CreateChannel();

    private GrpcChannel CreateChannel()
        => GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder
            .UseEnvironment("Development")
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(
                    new ForwardingLoggerProvider((logLevel, category, eventId, message, exception)
                        => LoggedMessage?.Invoke(logLevel, category, eventId, message, exception)));
            })
            .ConfigureServices(services =>
            {
                var dbContextDescriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<TagToolDbContext>));
                var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));

                services.Remove(dbContextDescriptor);
                services.Remove(dbConnectionDescriptor!);

                // Create open SqliteConnection so EF won't automatically close it.
                services.AddSingleton<DbConnection>(_ =>
                {
                    var testDbPath = Path.Combine(Path.GetTempPath(), "TagToolIntegrationTests");
                    var connection = new SqliteConnection($"DataSource={testDbPath}");
                    connection.Open();

                    return connection;
                });

                services.AddDbContext<ITagToolDbContext, TagToolDbContext>((container, options) =>
                {
                    var connection = container.GetRequiredService<DbConnection>();
                    options.UseSqlite(connection);
                });
            });
}
