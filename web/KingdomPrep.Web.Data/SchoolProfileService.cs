using System.Threading.Tasks;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public interface ISchoolProfileService
{
    Task<SchoolInfoEntity?> GetSchoolProfileAsync();
}

public class SchoolProfileService(IDbContextFactory<AppDbContext> dbFactory) : ISchoolProfileService
{
    private static bool _schemaVerified = false;

    public async Task<SchoolInfoEntity?> GetSchoolProfileAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        if (!_schemaVerified)
        {
            try
            {
                const string sqlAlt = @"
                    IF COL_LENGTH('SchoolInformation', 'Latitude') IS NULL
                        ALTER TABLE SchoolInformation ADD Latitude FLOAT NOT NULL CONSTRAINT DF_SI_Lat DEFAULT (5.6037);
                    IF COL_LENGTH('SchoolInformation', 'Longitude') IS NULL
                        ALTER TABLE SchoolInformation ADD Longitude FLOAT NOT NULL CONSTRAINT DF_SI_Lng DEFAULT (-0.1870);
                    IF COL_LENGTH('SchoolInformation', 'GeofenceRadiusMeters') IS NULL
                        ALTER TABLE SchoolInformation ADD GeofenceRadiusMeters FLOAT NOT NULL CONSTRAINT DF_SI_Radius DEFAULT (150.0);
                ";
                await db.Database.ExecuteSqlRawAsync(sqlAlt);
                _schemaVerified = true;
            }
            catch { /* Ignore if schema already upgraded or restricted */ }
        }
        // Since there is only one school profile row in the local schema, it's always Id 1
        return await db.SchoolInformation.FirstOrDefaultAsync(s => s.Id == 1);
    }
}
