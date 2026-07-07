using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EsportsTracker.Tests.Integration;

// Boots the real API (real controllers, real Npgsql/EF pipeline, real
// Program.cs Migrate() call) against whatever Postgres the connection
// string points at. Requires a reachable DB — see EsportsTracker.Tests README
// note / CI's postgres service container.
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // TournamentSyncService would otherwise start hitting the real
            // PandaScore/Polymarket APIs the moment the test host starts.
            services.RemoveAll<IHostedService>();
        });
    }
}
