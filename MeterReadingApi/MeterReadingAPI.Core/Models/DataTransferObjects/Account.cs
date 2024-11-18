namespace MeterReadingApi.Core.Models.DataTransferObjects;

public class Account
{
    public required string AccountNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}