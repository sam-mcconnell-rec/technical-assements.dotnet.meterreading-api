using MeterReadingApi.Core.Models.DataTransferObjects;

namespace MeterReadingApi.Core.Services;

public interface IMeterWriter
{
    public Task<(bool success, List<string> errorMessages)> PutMeterReading(MeterReading meterReading);

    public Task<(int accountsUploaded, int numberFailedValidation, List<string> errorMessages)> PutMeterReadings(List<MeterReading> meterReadings);
}