using System;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Core.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

class WebTest
{
    static async System.Threading.Tasks.Task Main()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLookup, UserLookupAdapter>();
        services.AddScoped<IAuthService, AuthService>();

        var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthService>();

        var res = await auth.AuthenticateAsync("suma", "admin");
        Console.WriteLine($"Auth success: {res != null}");
        if (res != null) {
            Console.WriteLine($"Role: {res.Role}");
        }
    }
}
