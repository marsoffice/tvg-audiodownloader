using System;
using System.Net.Http;
using System.Threading.Tasks;
using MarsOffice.Tvg.AudioDownloader.Abstractions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MarsOffice.Tvg.AudioDownloader
{
    public class RequestAudioBackgroundConsumer
    {
        private readonly HttpClient _httpClient;

        public RequestAudioBackgroundConsumer(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [FunctionName("RequestAudioBackgroundConsumer")]
        public async Task Run(
            [QueueTrigger("request-audiobackground", Connection = "localsaconnectionstring")] RequestAudioBackground request,
            [Queue("audiobackground-result", Connection = "localsaconnectionstring")] IAsyncCollector<AudioBackgroundResult> audioBackgroundResultQueue,
            ILogger log)
        {
            try
            {

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
                    UserId = request.UserId
                });
                await audioBackgroundResultQueue.FlushAsync();
            }
        }
    }
}
