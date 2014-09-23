using Kazyx.RemoteApi.AvContent;
using Kazyx.WPPMM.DataModel;
using System;
using System.Collections.Generic;

namespace Kazyx.WPPMM.PlaybackMode
{
    public class DummyContentsGenerator
    {
        private readonly Random random;
        private DummyContentsGenerator()
        {
            random = new Random();
        }

        private static DummyContentsGenerator INSTANCE = new DummyContentsGenerator();

        public static List<DateInfo> RandomDateList(int count)
        {
            var list = new List<DateInfo>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new DateInfo
                {
                    Title = YMDwithPadding(),
                    Uri = "dummyuri",
                });
                list.Sort((d1, d2) => { return string.CompareOrdinal(d2.Title, d1.Title); });
            }
            return list;
        }

        public static List<ContentInfo> RandomContentList(int count)
        {
            var list = new List<ContentInfo>();
            for (int i = 0; i < INSTANCE.random.Next(1, count); i++)
            {
                list.Add(new ContentInfo
                {
                    ContentType = ContentType(),
                    ThumbnailUrl = ThumbnailUrl(),
                    Name = FileName(),
                    CreatedTime = CreatedTime(),
                    LargeUrl = "http://upload.wikimedia.org/wikipedia/commons/e/e5/Earth_.jpg",
                });
            }
            return list;
        }

        public static string RandomUuid()
        {
            return "uuid:" + Guid.NewGuid().ToString();
        }

        private static readonly string[] dummyimages = new string[]{
            "http://one.htc.com/nyc/images/htc-one-gold-phone.png",
            "http://cdn.gsmarena.com/vv/newsimg/13/12/htc-one-max-black/gsmarena_001.jpg",
            "http://www.notebookcheck.net/fileadmin/_processed_/csm_Nokia-Lumia-720-3__2__d354fb1d00.jpg",
            "http://www.technobuffalo.com/wp-content/uploads/2013/05/Verizon-Nokia-Lumia-928-VS-Nokia-Lumia-920-Front.jpg",
            "http://cdn.gsmarena.com/vv/newsimg/13/12/htc-one-max-black/gsmarena_001.jpg",
        };

        private static string CreatedTime()
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        private static string FileName()
        {
            return "DUMMYFILE_" + INSTANCE.random.Next(0, 10000);
        }

        private static string ThumbnailUrl()
        {
            return dummyimages[INSTANCE.random.Next(0, dummyimages.Length - 1)];
        }

        private static string ContentType()
        {
            return INSTANCE.random.NextDouble() > 0.1 ? ContentKind.StillImage : ContentKind.MovieMp4;
        }

        private static int Year()
        {
            return INSTANCE.random.Next(2000, 2014);
        }

        private static int Month()
        {
            return INSTANCE.random.Next(1, 12);
        }

        private static int Day()
        {
            return INSTANCE.random.Next(1, 28);
        }

        private static string YMDwithPadding()
        {
            var m = Month().ToString();
            if (m.Length == 1)
            {
                m = "0" + m;
            }
            var d = Day().ToString();
            if (d.Length == 1)
            {
                d = "0" + d;
            }
            return Year().ToString() + m + d;
        }
    }
}
