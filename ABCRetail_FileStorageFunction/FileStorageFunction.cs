using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using System.Net;

namespace FileStorageFunction
{
    public class ABCRetail_FileStorageFunction
    {
        private readonly ILogger<ABCRetail_FileStorageFunction> _logger;

        //constructor to initialize logger
        public ABCRetail_FileStorageFunction(ILogger<ABCRetail_FileStorageFunction> logger)
        {
            _logger = logger;
        }

        //****************
        //Code Attribution
        //The following coode was taken from StackOverflow:
        //Author: Amy Pimpely
        //Link: https://stackoverflow.com/questions/64710485/azure-trigger-function-for-file-share
        //****************

        //function to handle file upload
        [Function("UploadFileFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("FileShareFunction processing a request for a file.");

            //check if file is uploaded
            if (req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("No file uploaded.");
            }

            //get uploaded file
            var fileUpload = req.Form.Files[0];

            try
            {
                //file Share interaction using connectionString
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage"); 
                ShareClient share = new ShareClient(connectionString, "productshare"); 

                if (!await share.ExistsAsync())
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError); 
                }

                //get directory and file clients
                ShareDirectoryClient directory = share.GetDirectoryClient("uploads");
                ShareFileClient fileClient = directory.GetFileClient(fileUpload.FileName);

                //upload file
                using (var stream = fileUpload.OpenReadStream()) 
                {
                    await fileClient.CreateAsync(fileUpload.Length); 
                    await fileClient.UploadRangeAsync(new HttpRange(0, fileUpload.Length), stream); 
                }

                //success response
                return new OkObjectResult("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during file upload."); 
                return new StatusCodeResult(StatusCodes.Status500InternalServerError); 
            }
        }
    }
}