using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MeterReadingApi.DataAccess.Repository.Accounts;

namespace MeterReadingApi.DataAccess.Repository.MeterValues
{
    [Table("MeterValues", Schema = "EnergyAccounts")]
    public class PersistedMeterValue
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public DateTime MeterReadingDateTime { get; set; }

        /// <remarks>
        /// Left as a string for now, but could be converted to an int or other numeric value once the requirements have been clarified
        /// </remarks>
        public required string MeterVaLue { get; set; }

        public PersistedAccount Account { get; set; }
    }
}
