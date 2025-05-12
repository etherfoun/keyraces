using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace KeyRacesTextGenerator
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<TextGenerator>();
                })
                .Build();

            host.Run();
        }
    }
}