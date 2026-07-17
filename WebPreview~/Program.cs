using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Threading.Tasks;

namespace VirtualRoom.WebPreview
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<Room>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            await builder.Build().RunAsync();
        }
    }
}
