using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.DataAccessServices;

public interface IAccountsRepository
{
    Task<IEnumerable<Account>> GetAllAsync();

    Task<List<Account>> GetByAccountIdsAsync(IEnumerable<string> accountIds);

    Task<bool> UpsertAccount(Account account);

    Task BulkUpsertAccounts(IEnumerable<Account> accounts);
}