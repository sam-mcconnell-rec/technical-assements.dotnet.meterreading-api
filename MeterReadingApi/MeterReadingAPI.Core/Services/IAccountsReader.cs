using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.Services;

public interface IAccountsReader
{
    public Task<List<Account>> GetAllAccountsAsync();

    public Task<List<Account>> GetAccountsByAccountNumbersAsync(IEnumerable<string> accountNumbers);
}