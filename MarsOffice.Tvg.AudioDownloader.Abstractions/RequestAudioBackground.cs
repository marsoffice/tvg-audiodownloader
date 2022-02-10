using System;
using System.Collections.Generic;
using System.Text;

namespace MarsOffice.Tvg.AudioDownloader.Abstractions
{
    public class RequestAudioBackground
    {
        public string VideoId { get; set; }
        public string JobId { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string Category { get; set; }
    }
}
