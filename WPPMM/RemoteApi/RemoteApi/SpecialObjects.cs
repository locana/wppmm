
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

    /// <summary>
    /// Though response of getEvent is not an object, use this as an callback arugument for future extension.
    /// </summary>
    public class Event
    {
        public string[] AvailableApis { internal set; get; }
        public string CameraStatus { internal set; get; }
        public ZoomInfo ZoomInfo { internal set; get; }
        public bool LiveviewAvailable { internal set; get; }
        public StrStrArray PostviewSizeInfo { internal set; get; }
        public IntIntArray SelfTimerInfo { internal set; get; }
        public StrStrArray ShootModeInfo { internal set; get; }
    }
}
