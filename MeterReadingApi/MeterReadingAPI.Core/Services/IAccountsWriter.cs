using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.Services;

public interface IAccountsWriter
{
    Task<(bool success, List<string> errorMessages)> PutAccount(Account account);

    public Task<(int accountsUploaded, int numberFailedValidation, List<string> errorMessages)> PutAccounts(List<Account> accounts);

    public Task SeedDatabase();
}