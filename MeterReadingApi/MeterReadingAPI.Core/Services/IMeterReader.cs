using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.Services;

public interface IMeterReader
{
    public Task<List<MeterReading>> GetMeterReadingsByAccountNumberAsync(string accountNumber);
}