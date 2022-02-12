using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarsOffice.Tvg.AudioDownloader.Abstractions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarsOffice.Tvg.AudioDownloader
{
    public class RequestAudioBackgroundConsumer
    {
        private readonly IConfiguration _config;

        public RequestAudioBackgroundConsumer(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("RequestAudioBackgroundConsumer")]
        public async Task Run(
            [QueueTrigger("request-audiobackground", Connection = "localsaconnectionstring")] RequestAudioBackground request,
            [Queue("audiobackground-result", Connection = "localsaconnectionstring")] IAsyncCollector<AudioBackgroundResult> audioBackgroundResultQueue,
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

                if (blobs.Count == 0)
                {
                    throw new Exception("No audio files present on server");
                }

                var rand = new Random();
                int randomIndex = rand.Next(0, blobs.Count);

                var selectedBlob = blobs[randomIndex];

                var fileNameSplit = selectedBlob.StorageUri.PrimaryUri.LocalPath.ToString().Split("/");
                var fileName = fileNameSplit.Last();
                var sas = cloudStorageAccount.GetSharedAccessSignature(new SharedAccessAccountPolicy
                {
                    Permissions = SharedAccessAccountPermissions.Read,
                    Protocols = SharedAccessProtocol.HttpsOnly,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1),
                    ResourceTypes = SharedAccessAccountResourceTypes.Object,
                    Services = SharedAccessAccountServices.Blob,
                    SharedAccessStartTime = DateTimeOffset.UtcNow
                });

                await audioBackgroundResultQueue.AddAsync(new AudioBackgroundResult
                {
                    VideoId = request.VideoId,
                    Success = true,
                    JobId = request.JobId,
                    UserEmail = request.UserEmail,
                    UserId = request.UserId,
                    FileLink = selectedBlob.Uri.ToString() + sas,
                    Category = request.Category,
                    LanguageCode = request.LanguageCode,
                    FileName = fileName
                });
                await audioBackgroundResultQueue.FlushAsync();
            }
            catch (Exception e)
            {
                await audioBackgroundResultQueue.AddAsync(new AudioBackgroundResult
                {
                    VideoId = request.VideoId,
                    Success = false,
                    Error = e.Message,
                    JobId = request.JobId,
                    UserEmail = request.UserEmail,
                    UserId = request.UserId,
                    Category = request.Category,
                    LanguageCode = request.LanguageCode
                });
                await audioBackgroundResultQueue.FlushAsync();
            }
        }
    }
}
