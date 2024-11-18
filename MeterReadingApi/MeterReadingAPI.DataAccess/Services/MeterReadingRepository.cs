using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.Core.Models.DataTransferObjects;
using MeterReadingApi.DataAccess.Repository;
using MeterReadingApi.DataAccess.Repository.Accounts;
using Microsoft.EntityFrameworkCore;
using System;
using MeterReadingApi.DataAccess.Repository.MeterValues;

namespace MeterReadingApi.DataAccess.Services;

public class MeterReadingRepository(DbContextOptions<ApplicationDbContext> contextOptions) : IMeterReadingRepository
{
    protected record struct MeterValueCompositeKeyContainer
    {
        public string AccountNumber { get; set; }
        public DateTime MeterReadingDateTime { get; set; }
    }

    public async Task<List<MeterReading>> GetAllMeterReadingByAccountIdAsync(string accountNumber)
    {
        await using var db = new ApplicationDbContext(contextOptions);
        var persistedReadings = await db.MeterValues.Where(m => m.Account.AccountNumber == accountNumber)
            .Include(persistedMeterValue => persistedMeterValue.Account).ToListAsync();

        return persistedReadings.Select(persistedReading =>
            new MeterReading
            {
                AccountNumber = persistedReading.Account.AccountNumber,
                MeterReadingDateTime = persistedReading.MeterReadingDateTime,
                MeterVaLue = persistedReading.MeterVaLue
            }).ToList();
    }

    public async Task<MeterReading?> GetMeterReadingByAccountIdAndDateAsync(string accountNumber, DateTime dateTime)
    {
        await using var db = new ApplicationDbContext(contextOptions);
        var persistedReading = await db.MeterValues
            .Include(persistedMeterValue => persistedMeterValue.Account)
            .SingleOrDefaultAsync(m => m.Account.AccountNumber == accountNumber && m.MeterReadingDateTime.Date.Equals(dateTime.Date));

        if (persistedReading == null)
        {
            return null;
        }

        // TODO: create a mapping method to go from persisted meter value to reading model
        return new MeterReading
        {
            AccountNumber = persistedReading.Account.AccountNumber,
            MeterReadingDateTime = persistedReading.MeterReadingDateTime,
            MeterVaLue = persistedReading.MeterVaLue
        };
    }

    public async Task<(bool success, List<string> errorMessages)> UpsertMeterReading(MeterReading meterReading)
    {
        await using var db = new ApplicationDbContext(contextOptions);


        var persistedReading = await db.MeterValues
            .Include(persistedMeterValue => persistedMeterValue.Account)
            .SingleOrDefaultAsync(m => m.Account.AccountNumber == meterReading.AccountNumber && m.MeterReadingDateTime.Date.Equals(meterReading.MeterReadingDateTime.Date));

        if (persistedReading == null)
        {
            // Check that the account number exists and map it to the account id guid used in the database
            var accountNumbersAndIds = await FetchAndMapAccountNumbersToAccountIds(db, [meterReading.AccountNumber]);

            if (!accountNumbersAndIds.TryGetValue(meterReading.AccountNumber, out var accountId))
            {
                return (false, [$"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: No existing account number found."]);
            }

            persistedReading = new PersistedMeterValue
            {
                Id = Guid.NewGuid(),
                MeterVaLue = meterReading.MeterVaLue,
                AccountId = accountId,
                MeterReadingDateTime = meterReading.MeterReadingDateTime.Date
            };
            await db.MeterValues.AddAsync(persistedReading);
        }
        else
        {
            persistedReading.MeterVaLue = meterReading.MeterVaLue;
        }

        await db.SaveChangesAsync();
        return (true, []);
    }

    public async Task<(bool success, List<string> errorMessages)> BulkUpsertMeterReading(List<MeterReading> meterReadings)
    {
        var errors = new List<string>();
        // Assume meterReadings is unique by account number and date
        await using var db = new ApplicationDbContext(contextOptions);

        // To help entity framework use the struct to hold the values we need to find any existing records, this makes performing the keys.contains(...) possible in LINQ.
        var keys = meterReadings.ToList().Select(m => new MeterValueCompositeKeyContainer{ MeterReadingDateTime = m.MeterReadingDateTime, AccountNumber = m.AccountNumber}).ToList();
        var persistedReadings = await db.MeterValues
            .Include(persistedMeterValue => persistedMeterValue.Account)
            .Where(m => keys.Contains(new MeterValueCompositeKeyContainer { MeterReadingDateTime = m.MeterReadingDateTime, AccountNumber = m.Account.AccountNumber})).ToListAsync();
        
        // Map persisted to the incoming readings
        List<(MeterReading incomningMeterReading, PersistedMeterValue? persistedMeterValue)> combinedReadings = meterReadings.Select(m => (m,
            persistedReadings.SingleOrDefault(persistedReading =>
                persistedReading.Account.AccountNumber == m.AccountNumber &&
                persistedReading.MeterReadingDateTime.Date.Equals(m.MeterReadingDateTime.Date)))).ToList();

        // First handle new meter reading - i.e. there is no existing reading for that account number and date combo
        var newMeterReadings = combinedReadings.Where(r => r.persistedMeterValue == null).Select(m => m.incomningMeterReading).ToList();

        if (newMeterReadings.Any())
        {
            var distinctAccountNumbers = newMeterReadings.Select(m => m.AccountNumber).Distinct().ToList();
            var accountNumbersAndIds = await FetchAndMapAccountNumbersToAccountIds(db, distinctAccountNumbers);

            foreach (var newReading in newMeterReadings)
            {
                if (!accountNumbersAndIds.TryGetValue(newReading.AccountNumber, out var accountId))
                {
                    errors.Add($"Meter reading account number & date {newReading.AccountNumber} {newReading.MeterReadingDateTime:u}: No existing account number found.");
                    continue;
                }

                await db.MeterValues.AddAsync(new PersistedMeterValue
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    MeterReadingDateTime = newReading.MeterReadingDateTime.Date,
                    MeterVaLue = newReading.MeterVaLue
                });
            }
        }

        // Now update all the existing meter readings with the new value
        foreach (var combinedReading in combinedReadings.Where(m => m.persistedMeterValue != null))
        {
            combinedReading.persistedMeterValue!.MeterVaLue = combinedReading.incomningMeterReading.MeterVaLue;
        }
        
        await db.SaveChangesAsync();
        return (true, errors);

    }

    private static async Task<Dictionary<string, Guid>> FetchAndMapAccountNumbersToAccountIds(ApplicationDbContext dbContext, IEnumerable<string> accountNumbers)
    {
        // Assume accountNumbers are distinct
        var persistedAccounts = await dbContext.Accounts.Where(a => accountNumbers.Contains(a.AccountNumber)).ToListAsync();
        return persistedAccounts.ToDictionary(x => x.AccountNumber, x => x.Id);
    }
}