using System;
using System.Linq;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Core.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

class WebTest2
{
    static async System.Threading.Tasks.Task Main()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"));

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();

        var parents = await db.Users.Where(u => u.UserType == "PARENT" || u.UserType == "GUARDIAN").ToListAsync();
        Console.WriteLine($"Found {parents.Count} parents.");
        foreach(var p in parents) {
            Console.WriteLine($"Parent: {p.Username}, Linked ID: {p.EmploymentID}");
        }
    }
}
