using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.DataAccessServices;

public interface IMeterReadingRepository
{
    Task<List<MeterReading>> GetAllMeterReadingByAccountIdAsync(string accountNumber);

    Task<MeterReading?> GetMeterReadingByAccountIdAndDateAsync(string accountNumber, DateTime dateTime);

    Task<(bool success, List<string> errorMessages)> UpsertMeterReading(MeterReading meterReading);

    Task<(bool success, List<string> errorMessages)> BulkUpsertMeterReading(List<MeterReading> meterReadings);
}