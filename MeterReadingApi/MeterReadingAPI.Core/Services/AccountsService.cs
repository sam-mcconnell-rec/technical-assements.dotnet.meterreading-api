using System.Text;
using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.Core.Models.DataTransferObjects;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MeterReadingApi.Core.Services;

public class AccountsService(IAccountsRepository accountsRepository) : IAccountsReader, IAccountsWriter
{
    // Crude way to track if initialization has happened
    private static bool _hasSeededDatabase = false;


    public async Task<(bool success, List<string> errorMessages)> PutAccount(Account account)
    {
        if (_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        var (isValid, errors) = ValidateAccount(account);

        if (!isValid)
        {
            return (false, errors);
        }

        var result = await accountsRepository.UpsertAccount(account);
        return (result, new List<string>());
    }

    public async Task<(int accountsUploaded, int numberFailedValidation, List<string> errorMessages)> PutAccounts(List<Account> accounts)
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        var skippedAccounts = 0;
        var checkedAccounts = new List<Account>();
        var uniqueAccountNumbers = new HashSet<string>();
        var errors = new List<string>();
        foreach (var account in accounts)
        {
            var (isValid, validationErrors) = ValidateAccount(account);

            if (!uniqueAccountNumbers.Add(account.AccountNumber))
            {
                validationErrors.Add($"Account {account.AccountNumber}: AccountNumber already in list to be uploaded. List must contain no duplicates");
                isValid = false;
            }

            if (!isValid)
            {
                errors.AddRange(validationErrors);
                skippedAccounts++;
                continue;
            }
            checkedAccounts.Add(account);
        }

        if (checkedAccounts.Count > 0)
        {
            await accountsRepository.BulkUpsertAccounts(checkedAccounts);
        }

        return (checkedAccounts.Count, skippedAccounts, errors);
    }

    public async Task<List<Account>> GetAllAccountsAsync()
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        return (await accountsRepository.GetAllAsync()).ToList();
    }

    public async Task<List<Account>> GetAccountsByAccountNumbersAsync(IEnumerable<string> accountNumbers)
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        return (await accountsRepository.GetByAccountIdsAsync(accountNumbers)).ToList();
    }

    private static (bool isValid, List<string> errorMessages) ValidateAccount(Account account)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(account.AccountNumber))
        {
            errors.Add("AccountNumber must have a value");
        }
        if (string.IsNullOrWhiteSpace(account.FirstName))
        {
            errors.Add($"Account {account.AccountNumber}: FirstName must have a value");
        }
        if (string.IsNullOrWhiteSpace(account.LastName))
        {
            errors.Add($"Account {account.AccountNumber}: LastName must have a value");
        }

        return (errors.Count == 0, errors);
    }



    public async Task SeedDatabase()
    {
        if (_hasSeededDatabase)
        {
            return;
        }
        // Set up initial accounts
        var accounts = new List<Account>
        {
            new Account
            {
                AccountNumber = "123",
                FirstName = "Sam",
                LastName = "McConnell"
            },
            new Account
            {
                AccountNumber = "321",
                FirstName = "Test First Name 1",
                LastName = "Test Last Name 1"
            },
            new Account
            {
                AccountNumber = "111",
                FirstName = "Test First Name 2",
                LastName = "Test Last Name 2"
            }
        };

        await accountsRepository.BulkUpsertAccounts(accounts);
        _hasSeededDatabase = true;
    }
}