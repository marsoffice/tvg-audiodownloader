using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarsOffice.Microfunction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarsOffice.Tvg.AudioDownloader
{
    public class AudioBackgrounds
    {
        private readonly IConfiguration _config;

        public AudioBackgrounds(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("AudioBackgrounds")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/audiodownloader/audioBackgrounds")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(_config["localsaconnectionstring"]);
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();
                var containerReference = blobClient.GetContainerReference("audio");
#if DEBUG
                await containerReference.CreateIfNotExistsAsync();
#endif
                BlobContinuationToken bct = null;
                var hasData = true;
                var blobs = new List<IListBlobItem>();

                while (hasData)
                {
                    var allFilesInContainer = await containerReference.ListBlobsSegmentedAsync(bct);
                    blobs.AddRange(allFilesInContainer.Results);
                    bct = allFilesInContainer.ContinuationToken;
                    if (bct == null)
                    {
                        hasData = false;
                    }
                }

                return new OkObjectResult(
                    blobs.Select(x => x.StorageUri.PrimaryUri.LocalPath.ToString().Split("/").Last()).ToList()
                );
            }
            catch (Exception e)
            {
                log.LogError(e, "Exception occured in function");
                return new BadRequestObjectResult(Errors.Extract(e));
            }
        }
    }
}
