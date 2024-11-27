using FluentAssertions;
using MeterReadingApi.Core.DataAccessServices;
using MeterReadingApi.Core.Models.DataTransferObjects;
using MeterReadingApi.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MeterReadingApi.Tests.Core.Services;

[TestClass]
public class MeterServiceTests
{
    private Mock<IMeterReadingRepository> _meterReadingRepositoryMock;
    private Mock<IAccountsWriter> _accountsWriterMock;
    private MeterService _meterService;

    [TestInitialize]
    public void Setup()
    {
        _meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
        _accountsWriterMock = new Mock<IAccountsWriter>();
        _meterService = new MeterService(_meterReadingRepositoryMock.Object, _accountsWriterMock.Object);
    }

    [TestMethod]
    public async Task when_account_number_is_null_or_whitespace_then_throws_argument_exception()
    {
        Func<Task> act = async () => await _meterService.GetMeterReadingsByAccountNumberAsync(null);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("accountNumber must have a value (Parameter 'accountNumber')");

        act = async () => await _meterService.GetMeterReadingsByAccountNumberAsync(" ");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("accountNumber must have a value (Parameter 'accountNumber')");
    }

    [TestMethod]
    public async Task when_account_number_is_valid_then_returns_meter_readings()
    {
        var accountNumber = "123";
        var meterReadings = new List<MeterReading>
        {
            new MeterReading { AccountNumber = accountNumber, MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "12345" }
        };

        _meterReadingRepositoryMock.Setup(repo => repo.GetAllMeterReadingByAccountIdAsync(accountNumber))
            .ReturnsAsync(meterReadings);

        var result = await _meterService.GetMeterReadingsByAccountNumberAsync(accountNumber);

        result.Should().BeEquivalentTo(meterReadings);
    }

    [TestMethod]
    public async Task when_meter_reading_is_valid_then_upserts_meter_reading()
    {
        var meterReading = new MeterReading { AccountNumber = "123", MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "12345" };
        _meterReadingRepositoryMock.Setup(repo => repo.UpsertMeterReading(meterReading))
            .ReturnsAsync((true, new List<string>()));

        var result = await _meterService.PutMeterReading(meterReading);

        result.success.Should().BeTrue();
        result.errorMessages.Should().BeEmpty();
    }

    [TestMethod]
    public async Task when_meter_reading_is_invalid_then_returns_error_messages()
    {
        var meterReadingDateTime = DateTime.UtcNow;
        var meterReading = new MeterReading { AccountNumber = "", MeterReadingDateTime = meterReadingDateTime, MeterVaLue = "123456" };

        var result = await _meterService.PutMeterReading(meterReading);

        result.success.Should().BeFalse();
        result.errorMessages.Should().Contain("AccountNumber must have a value");
        result.errorMessages.Should().Contain($"Meter reading account number & date  {meterReadingDateTime:u}: MeterVaLue (123456) cannot have more than 5 digits");
    }

    [TestMethod]
    public async Task when_meter_readings_are_valid_then_bulk_upserts_meter_readings()
    {
        var meterReadings = new List<MeterReading>
        {
            new MeterReading { AccountNumber = "123", MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "12345" },
            new MeterReading { AccountNumber = "124", MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "54321" }
        };

        _meterReadingRepositoryMock.Setup(repo => repo.BulkUpsertMeterReading(meterReadings))
            .ReturnsAsync((true, new List<string>()));

        var result = await _meterService.PutMeterReadings(meterReadings);

        result.accountsUploaded.Should().Be(2);
        result.numberFailedValidation.Should().Be(0);
        result.errorMessages.Should().BeEmpty();
    }

    [TestMethod]
    public async Task when_meter_readings_have_duplicates_then_returns_error_messages()
    {
        _meterReadingRepositoryMock.Setup(repo => repo.BulkUpsertMeterReading(It.IsAny<List<MeterReading>>()))
            .ReturnsAsync((true, new List<string>()));
        var meterReadings = new List<MeterReading>
        {
            new MeterReading { AccountNumber = "123", MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "12345" },
            new MeterReading { AccountNumber = "123", MeterReadingDateTime = DateTime.UtcNow, MeterVaLue = "54321" }
        };

        var result = await _meterService.PutMeterReadings(meterReadings);

        result.accountsUploaded.Should().Be(1);
        result.numberFailedValidation.Should().Be(1);
        result.errorMessages.Count.Should().Be(1);
        result.errorMessages[0].Should().Contain("Meter reading account number & date");
    }
}