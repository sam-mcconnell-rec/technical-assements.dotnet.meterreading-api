using MeterReadingApi.Core.Models.DataTransferObjects;
using MeterReadingApi.Core.Services;
using MeterReadingApi.RestfulAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace MeterReadingApi.RestfulAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController(IAccountsReader accountsReader, IAccountsWriter accountsWriter) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllAccountsAsync()
        {
            var allAccounts = await accountsReader.GetAllAccountsAsync();
            return Ok(allAccounts);
        }

        [HttpPut]
        public async Task<IActionResult> SaveAccount(Account account)
        {
            var result = await accountsWriter.PutAccount(account);

            if (result.success)
            {
                return Ok(account);
            }

            return BadRequest(result.errorMessages);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAccountsFile(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            const int skipAtStart = 1;
            const int expectedParts = 3;
            const char delimiter = ',';
            var (skippedLines, parsedAccounts, errors) = await file.ParseCvsFile<Account>(parts =>
                {
                    if (parts.Count != expectedParts)
                    {
                        return (false, (Account?)null, [$"Number of line parts ({parts.Count}) not equal to expected ({expectedParts})"]);
                    }

                    var account = new Account()
                    {
                        AccountNumber = parts[0],
                        FirstName = parts[1],
                        LastName = parts[2]
                    };

                    return (true, account, []);

                },
                delimiter,
                skipAtStart);

            if (parsedAccounts.Count == 0)
            {
                return Ok(new
                {
                    Succeeded = 0,
                    Failed = skippedLines,
                    errors = errors
                });
            }

            var results = await accountsWriter.PutAccounts(parsedAccounts);
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
