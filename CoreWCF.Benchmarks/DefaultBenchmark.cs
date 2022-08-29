using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using CoreWCF.Configuration;
using LibGit2Sharp;
using Xunit;

namespace CoreWCF.Benchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser(true)]
[KeepBenchmarkFiles]
public class DefaultBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            string repositoryPath = Repository.Discover(@"c:\src\g7ed6e\CoreWCF");
            using var repo = new Repository(repositoryPath);
            string currentBranchName  = repo.Head.FriendlyName;

            AddJob(Job.Dry.WithGcServer(true)
                .WithId(currentBranchName));
            
            AddJob(Job.Dry.WithGcServer(true)
                .WithEnvironmentVariable("CoreWCFBasePath", "")
                .AsBaseline());
        }
    }
    
    private HttpClient client;

    [GlobalSetup]
    public void Setup()
    {
        var factory = new IntegrationTest<Startup>();
        client = factory.CreateClient();
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        client.Dispose();
    }

    [Benchmark]
    [Fact]
    public async Task Buffered()
    {
        const string action = "http://tempuri.org/IEchoService/Echo";
            
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("http://localhost:8080/BasicWcfService/basichttp.svc", UriKind.Absolute));
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{action}\"");

        const string requestBody = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"">
   <s:Header/>
   <s:Body>
      <tem:Echo>
         <tem:text>A</tem:text>
      </tem:Echo>
   </s:Body>
</s:Envelope>";
            
        request.Content = new StringContent(requestBody, Encoding.UTF8, "text/xml");

        // FIXME: Commenting out this line will induce a chunked response, which will break the pre-read message parser
        request.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(requestBody);

        var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode);
        
        var responseBody = await response.Content.ReadAsStringAsync();

        const string expected = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                                "<s:Body>" +
                                "<EchoResponse xmlns=\"http://tempuri.org/\">" +
                                "<EchoResult>A</EchoResult>" +
                                "</EchoResponse>" +
                                "</s:Body>" +
                                "</s:Envelope>";
        
        Assert.Equal(expected, responseBody);
    }
    
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseServiceModel(builder =>
            {
                builder.AddService<EchoService>();
                builder.AddServiceEndpoint<EchoService, IEchoService>(new BasicHttpBinding(), "/BasicWcfService/basichttp.svc");
            });
        }
    }
    
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class EchoService : IEchoService
    {
        public string Echo(string text) => text;
    }
    
    [ServiceContract]
    public interface IEchoService
    {
        [OperationContract]
        string Echo(string text);
    }
}