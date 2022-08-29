using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore;
using Xunit.Abstractions;

namespace CoreWCF.Benchmarks;

public static class ServiceHelper
{
    public static IWebHostBuilder CreateWebHostBuilder<TStartup>(ITestOutputHelper outputHelper = default) where TStartup : class =>
        WebHost.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 8080, listenOptions =>
                {
                    if (Debugger.IsAttached)
                    {
                        listenOptions.UseConnectionLogging();
                    }
                });
            })
            .UseStartup<TStartup>();
}