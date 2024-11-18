using MeterReadingApi.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeterReadingApi.Core;

public class MeterReadingApiCore
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.TryAddScoped<IAccountsReader, AccountsService>();
        services.TryAddScoped<IAccountsWriter, AccountsService>();
        services.TryAddScoped<IMeterReader, MeterService>();
        services.TryAddScoped<IMeterWriter, MeterService>();
    }
}