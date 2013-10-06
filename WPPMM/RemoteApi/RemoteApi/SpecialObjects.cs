
namespace WPPMM.RemoteApi
{
    public class ZoomInfo
    {
        public int position { set; get; }
        public int number_of_boxes { set; get; }
        public int current_box_index { set; get; }
        public int position_in_current_box { set; get; }
    }

    public class StrStrArray
    {
        public string current { set; get; }
        public string[] candidates { set; get; }
    }

    public class IntIntArray
    {
        public int current { set; get; }
        public int[] candidates { set; get; }
    }

    public class MethodType
    {
        public string name { set; get; }
        public string[] reqtypes { set; get; }
        public string[] restypes { set; get; }
        public string version { set; get; }
    }
}
