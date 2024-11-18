namespace MeterReadingApi.Core.Models.DataTransferObjects
{
    public class MeterReading
    {
        public required string AccountNumber { get; set; }
        public required DateTime MeterReadingDateTime { get; set; }
        public required string MeterVaLue { get; set; }
    }
}
