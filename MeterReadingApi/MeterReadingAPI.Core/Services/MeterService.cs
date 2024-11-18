using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.Services;

public class MeterService(IMeterReadingRepository meterReadingRepository, IAccountsWriter accountsWriter) : IMeterReader, IMeterWriter
{
    // Crude way to track if initialization has happened
    private static bool _hasSeededDatabase = false;

    public async Task<List<MeterReading>> GetMeterReadingsByAccountNumberAsync(string accountNumber)
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new ArgumentException("accountNumber must have a value", nameof(accountNumber));
        }

        return await meterReadingRepository.GetAllMeterReadingByAccountIdAsync(accountNumber);
    }

    public async Task<(bool success, List<string> errorMessages)> PutMeterReading(MeterReading meterReading)
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        var (isValid, errors) = ValidateMeterReading(meterReading);
        if (!isValid)
        {
            return (isValid, errors);
        }
        var result = await meterReadingRepository.UpsertMeterReading(meterReading);

        return (result.success, result.errorMessages);

    }

    public async Task<(int accountsUploaded, int numberFailedValidation, List<string> errorMessages)> PutMeterReadings(List<MeterReading> meterReadings)
    {
        if (!_hasSeededDatabase)
        {
            await SeedDatabase();
        }

        var skippedMeterReadings = 0;
        var checkedMeterReadings = new List<MeterReading>();
        var uniqueAccountNumbers = new HashSet<(string, DateTime)>();
        var errors = new List<string>();
        foreach (var meterReading in meterReadings)
        {
            var (isValid, validationErrors) = ValidateMeterReading(meterReading);

            if (!uniqueAccountNumbers.Add((meterReading.AccountNumber, meterReading.MeterReadingDateTime.Date)))
            {
                validationErrors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: Meter Reading with the same account number and date is already in list to be uploaded. List must contain no duplicates. Only the date part of the date time matters.");
                isValid = false;
            }

            if (!isValid)
            {
                errors.AddRange(validationErrors);
                skippedMeterReadings++;
                continue;
            }
            checkedMeterReadings.Add(meterReading);
        }

        if (checkedMeterReadings.Count > 0)
        {
            
            var (_, dataAccessErrors) = await meterReadingRepository.BulkUpsertMeterReading(checkedMeterReadings);
            errors.AddRange(dataAccessErrors);
        }

        return (checkedMeterReadings.Count, skippedMeterReadings, errors);
    }

    private static (bool isValid, List<string> errorMessages) ValidateMeterReading(MeterReading meterReading)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(meterReading.AccountNumber))
        {
            errors.Add("AccountNumber must have a value");
        }
        if (string.IsNullOrWhiteSpace(meterReading.MeterVaLue))
        {
            errors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: MeterVaLue must have a value");
        }
        // Specification is unclear, it might be allowable for the field to have trailing zeros but I've made assumptions:
        // the field can have a maximum of 5 characters, and could allow trailing zeros. It should be parsable to an int and not negative.
        if (meterReading.MeterVaLue.Length > 5) 
        {
            errors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: MeterVaLue ({meterReading.MeterVaLue}) cannot have more than 5 digits");
        }
        if (!int.TryParse(meterReading.MeterVaLue, out var parsedMeterReading))
        {
            errors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: MeterVaLue ({meterReading.MeterVaLue}) could not be parsed to a number");
        }
        else
        {
            if (parsedMeterReading < 0)
            {
                errors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: MeterVaLue ({meterReading.MeterVaLue}) cannot be negative");
            }
        }

        if (meterReading.MeterReadingDateTime.ToUniversalTime().Date > DateTime.UtcNow.Date)
        {
            errors.Add($"Meter reading account number & date {meterReading.AccountNumber} {meterReading.MeterReadingDateTime:u}: MeterReadingDateTime cannot be for a future date");
        }

        return (errors.Count == 0, errors);
    }
    

    public async Task SeedDatabase()
    {
        if (_hasSeededDatabase)
        {
            return;
        }
        // First make sure the accounts db has been seeded
        await accountsWriter.SeedDatabase();

        // Set up initial readings
        var readings = new List<MeterReading>
        {
            new MeterReading
            {
                AccountNumber = "123",
                MeterReadingDateTime = new DateTime(2020, 1, 1),
                MeterVaLue = "12345"
            },
            new MeterReading
            {
                AccountNumber = "123",
                MeterReadingDateTime = new DateTime(2021, 12, 31),
                MeterVaLue = "54321"
            },
            new MeterReading
            {
                AccountNumber = "123",
                MeterReadingDateTime = new DateTime(2022, 6, 6),
                MeterVaLue = "99999"
            },
            new MeterReading
            {
                AccountNumber = "111",
                MeterReadingDateTime = new DateTime(2020, 1, 1),
                MeterVaLue = "00001"
            }
        };


        await meterReadingRepository.BulkUpsertMeterReading(readings);
        _hasSeededDatabase = true;
    }

}