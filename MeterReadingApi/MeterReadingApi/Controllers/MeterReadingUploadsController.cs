using MeterReadingApi.Core.Models.DataTransferObjects;
using MeterReadingApi.Core.Services;
using MeterReadingApi.RestfulAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace MeterReadingApi.RestfulAPI.Controllers
{
    // localhost:xxxx/api/meter-reading-uploads
    [Route("api/meter-reading-uploads")]
    [ApiController]
    public class MeterReadingUploadsController(IMeterReader meterReader, IMeterWriter meterWriter) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetAllMeterReadings(string accountNumber)
        {

            var allMeterReadings = await meterReader.GetMeterReadingsByAccountNumberAsync(accountNumber);
            return Ok(allMeterReadings);
        }

        [HttpPut]
        public async Task<IActionResult> SaveMeterReading(MeterReading meterReading)
        {
            var result = await meterWriter.PutMeterReading(meterReading);

            if (result.success)
            {
                return Ok(meterReading);
            }

            return BadRequest(meterReading);
        }

        [HttpPost]
        public async Task<IActionResult> UploadMeterReadingsFile(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            const int skipAtStart = 1;
            const int expectedParts = 3;
            const char delimiter = ',';
            var (skippedLines, parsedMeterReadings, errors) = await file.ParseCvsFile<MeterReading>(parts =>
                {
                    // The test file provided has an empty cell at the end of every line - so for this file parsing additional cells (empty or otherwise) will be ignored and will not generate errors. This will not cause issues later anyway.
                    if (parts.Count < expectedParts)
                    {
                        return (false, (MeterReading?)null, [$"Number of line parts ({parts.Count}) not equal to expected ({expectedParts})"]);
                    }

                    var rawDateTimeText = parts[1];
                    if (!DateTime.TryParse((string?)rawDateTimeText, out var parsedDateTime))
                    {
                        return (false, (MeterReading?)null, [$"Could not parse value (\"{rawDateTimeText}\") to date time"]);
                    }

                    // Assume UTC values for all date times
                    if (parsedDateTime.Kind == DateTimeKind.Unspecified)
                    {
                        parsedDateTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc);
                    }

                    var meterReading = new MeterReading()
                    {
                        AccountNumber = parts[0],
                        MeterReadingDateTime = parsedDateTime,
                        MeterVaLue = parts[2]
                    };

                    return (true, meterReading, []);

                },
                delimiter,
                skipAtStart);

            if (parsedMeterReadings.Count == 0)
            {
                return Ok(new
                {
                    Succeeded = 0,
                    Failed = skippedLines,
                    errors = errors
                });
            }

            var results = await meterWriter.PutMeterReadings(parsedMeterReadings);
            errors.AddRange(results.errorMessages);
            return Ok(new
            {
                Succeeded = results.accountsUploaded,
                Failed = results.numberFailedValidation + skippedLines,
                errors = errors
            });
        }
    }
}
