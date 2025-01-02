using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Shared;

public static class PgSqlTransportExtensions
{
    public static IServiceCollection ConfigureMassTransitWithPostgresTransport(this IServiceCollection services,
        string? connectionString, Action<IBusRegistrationConfigurator>? configure = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.Host = builder.Host ?? "localhost";
            options.Database = builder.Database ?? "orphan";
            options.Schema = "transport";
            options.Role = "transport";
            options.Username = "masstransit";
            options.Password = "H4rd2Gu3ss!";
            options.AdminUsername = builder.Username;
            options.AdminPassword = builder.Password;
        });

        services.AddPostgresMigrationHostedService();


        services.AddMassTransit(x =>
        {
            x.AddSqlMessageScheduler();

            x.SetKebabCaseEndpointNameFormatter();

            x.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                if (cfg is ISqlReceiveEndpointConfigurator sql)
                    sql.SetReceiveMode(SqlReceiveMode.PartitionedOrdered);
            });

            configure?.Invoke(x);

            x.UsingPostgres((context, cfg) =>
            {
                cfg.UseSqlMessageScheduler();

                cfg.SendTopology.UsePartitionKeyFormatter<TestMessage>(p => p.Message.PartitionKey);

                cfg.AutoStart = true;

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddOptions<MassTransitHostOptions>()
            .Configure(options =>
            {
                options.WaitUntilStarted = true;
                options.StartTimeout = TimeSpan.FromSeconds(10);
                options.StopTimeout = TimeSpan.FromSeconds(30);
                options.ConsumerStopTimeout = TimeSpan.FromSeconds(10);
            });
        services.AddOptions<HostOptions>()
            .Configure(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1));

        return services;
    }
}