using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MeterReadingApi.DataAccess.Repository.MeterValues;

namespace MeterReadingApi.DataAccess.Repository.Accounts;

[Table("MeterValues", Schema = "EnergyAccounts")]
public class PersistedAccount
{
    [Key]
    public Guid Id { get; set; }
    public required string AccountNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public ICollection<PersistedMeterValue> MeterValues { get; set; }
}