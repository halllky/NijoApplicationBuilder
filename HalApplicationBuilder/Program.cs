using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HalApplicationBuilder {
    public class Program {

        static void Main(string[] args) {
            var serviceCollection = new ServiceCollection();
            var assembly = Assembly.LoadFile("/__local__/20221211_haldoc_csharp/haldoc/HalApplicationBuilderSampleSchema/bin/Debug/net5.0/HalApplicationBuilderSampleSchema.dll");
            HalApp.Configure(serviceCollection, assembly);

            serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<HalApp>()
                .GenerateCode(Console.Out);
        }
    }
}
