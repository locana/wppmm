using System.ComponentModel;

namespace Kazyx.WPPMM.DataModel
{
    public class DateInfo
    {
        public string Title { set; get; }
        public string Uri { set; get; }
    }

    public class ContentInfo
    {
        /// <summary>
        /// File name without extension.
        /// </summary>
        public string Name { set; get; }
        /// <summary>
        /// Only Jpeg original url will be set if it is available.
        /// </summary>
        public string OriginalUrl { set; get; }
        public string LargeUrl { set; get; }
        public string ThumbnailUrl { set; get; }
        public string Uri { set; get; }
        public string ContentType { set; get; }
        public string CreatedTime { set; get; }
        public bool Protected { set; get; }
        public bool RemotePlaybackAvailable { set; get; }
    }
}
