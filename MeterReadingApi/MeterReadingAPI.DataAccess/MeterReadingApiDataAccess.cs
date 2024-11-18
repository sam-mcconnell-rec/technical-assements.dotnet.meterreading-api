using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.DataAccess.Repository;
using MeterReadingApi.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeterReadingApi.DataAccess;

public class MeterReadingApiDataAccess
{
    public static void ConfigureServices(IServiceCollection services, string dbConnectionString)
    {
        services.TryAddScoped<IAccountsRepository, AccountRepository>();
        services.TryAddScoped<IMeterReadingRepository, MeterReadingRepository>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (dbConnectionString == "UseInMemory")
            {
                options.UseInMemoryDatabase("ensek-energyaccounts");
            }
            else
            {
                options.UseSqlServer(dbConnectionString);
            }
        });
    }
}