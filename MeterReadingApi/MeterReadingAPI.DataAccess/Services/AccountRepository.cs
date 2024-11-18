using MeterReadingApi.Core;
using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.Core.Models.DataTransferObjects;
using MeterReadingApi.DataAccess.Repository;
using MeterReadingApi.DataAccess.Repository.Accounts;
using Microsoft.EntityFrameworkCore;

namespace MeterReadingApi.DataAccess.Services;

public class AccountRepository(DbContextOptions<ApplicationDbContext> contextOptions) : IAccountsRepository
{
    public async Task<IEnumerable<Account>> GetAllAsync()
    {
        await using var db = new ApplicationDbContext(contextOptions);

        var allPersistedAccounts = await db.Accounts.ToListAsync();

        return allPersistedAccounts.Select(pa => new Account
        {
            AccountNumber = pa.AccountNumber,
            FirstName = pa.FirstName,
            LastName = pa.LastName
        });
    }

    public async Task<List<Account>> GetByAccountIdsAsync(IEnumerable<string> accountIds)
    {
        await using var db = new ApplicationDbContext(contextOptions);
        var persistedAccounts = await db.Accounts.Where(a => accountIds.Contains(a.AccountNumber)).ToListAsync();

        // TODO: create a mapping method to go from persisted account to account model
        return persistedAccounts.Select(persistedAccount =>
            new Account
            {
                AccountNumber = persistedAccount.AccountNumber,
                FirstName = persistedAccount.FirstName,
                LastName = persistedAccount.LastName
            }).ToList();
    }

    public async Task<bool> UpsertAccount(Account account)
    {
        if (string.IsNullOrWhiteSpace(account.AccountNumber) || string.IsNullOrWhiteSpace(account.FirstName) || string.IsNullOrWhiteSpace(account.LastName))
        {
            return false;
        }

        await using var db = new ApplicationDbContext(contextOptions);

        // Fetch any existing account details
        var persistedAccount = await db.Accounts.SingleOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);
        if (persistedAccount == null)
        {
            persistedAccount = new PersistedAccount
            {
                Id = Guid.NewGuid(),
                AccountNumber = account.AccountNumber,
                FirstName = account.FirstName,
                LastName = account.LastName
            };
            await db.Accounts.AddAsync(persistedAccount);
        }
        else
        {
            persistedAccount.FirstName = account.FirstName;
            persistedAccount.LastName = account.LastName;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task BulkUpsertAccounts(IEnumerable<Account> accounts)
    {
        await BulkUpsertAccounts(accounts.ToList());
    }

    private async Task BulkUpsertAccounts(List<Account> accounts)
    {
        // Very crudely just load all accounts
        // In the real world this should be preformed in batches, checking the relevant ids - or use proper upsert functionality in SQL.
        await using var db = new ApplicationDbContext(contextOptions);

        var allPersistedAccounts = await db.Accounts.ToListAsync();

        var mappedAccounts = accounts.Select(account => (account, allPersistedAccounts.SingleOrDefault(pa => pa.AccountNumber == account.AccountNumber)));
        foreach (var (incomingAccount, persistedAccount) in mappedAccounts)
        {
            if (persistedAccount == null)
            { 
                var newPersistedAccount = new PersistedAccount
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = incomingAccount.AccountNumber,
                    FirstName = incomingAccount.FirstName,
                    LastName = incomingAccount.LastName
                };
                db.Accounts.Add(newPersistedAccount);
            }
            else
            {
                persistedAccount.FirstName = incomingAccount.FirstName;
                persistedAccount.LastName = incomingAccount.LastName;
            }
        }

        await db.SaveChangesAsync();
    }
}