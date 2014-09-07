using System.ComponentModel;

namespace Kazyx.WPMMM.DataModel
{
    public class DateInfo
    {
        public string Title { set; get; }
        public string Uri { set; get; }
    }

    public class ContentInfo
    {
        public string Name { set; get; }
        public string LargeUrl { set; get; }
        public string ThumbnailUrl { set; get; }
        public string Uri { set; get; }
        public string ContentType { set; get; }
    }
}
