
namespace WPPMM.RemoteApi
{
    public class ZoomInfo
    {
        public int position { set; get; }
        public int number_of_boxes { set; get; }
        public int current_box_index { set; get; }
        public int position_in_current_box { set; get; }
    }

    /// <summary>
    /// Set of current value and its candidates.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BasicInfo<T>
    {
        public T current { set; get; }
        public T[] candidates { set; get; }
    }

    public class MethodType
    {
        public string name { set; get; }
        public string[] reqtypes { set; get; }
        public string[] restypes { set; get; }
        public string version { set; get; }
    }

    public class ApplicationInfo
    {
        public string name { set; get; }
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
        public BasicInfo<string> PostviewSizeInfo { internal set; get; }
        public BasicInfo<int> SelfTimerInfo { internal set; get; }
        public BasicInfo<string> ShootModeInfo { internal set; get; }
        public BasicInfo<string> ExposureMode { internal set; get; }
        public BasicInfo<string> ShutterSpeed { internal set; get; }
        public BasicInfo<string> ISOSpeedRate { internal set; get; }
        public BasicInfo<string> FNumber { internal set; get; }
        public EvInfo EvInfo { internal set; get; }
        public bool? ProgramShiftActivated { internal set; get; }
    }

    public class EvInfo
    {
        public int CurrentIndex { internal set; get; }
        public EvRange Range { internal set; get; }
    }

    public class SetAFResult
    {
        public bool Focused { internal set; get; }
        public string Mode { internal set; get; }
    }

    public class EvRange
    {
        public EvStepDefinition IndexStep { internal set; get; }
        public int MaxIndex { internal set; get; }
        public int MinIndex { internal set; get; }
    }
}
