using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace CoreWCF.Benchmarks;

public class IntegrationTest<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        var addresses = new ServerAddressesFeature();
        addresses.Addresses.Add("http://localhost/");
        var features = new FeatureCollection();
        features.Set<IServerAddressesFeature>(addresses); 

        var server = new TestServer(builder, features);
        return server;
    }

    protected override IWebHostBuilder CreateWebHostBuilder()
    {
        SetSelfHostedContentRoot();

        return ServiceHelper.CreateWebHostBuilder<TStartup>();
    }

    private static void SetSelfHostedContentRoot()
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var assemblyName = typeof(IntegrationTest<TStartup>).Assembly.GetName().Name;
        var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_");
        var settingName = $"ASPNETCORE_TEST_CONTENTROOT_{settingSuffix}";
        Environment.SetEnvironmentVariable(settingName, contentRoot);
    }
}