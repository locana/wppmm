
namespace WPPMM.RemoteApi
{
    public delegate void MethodTypesHandler(MethodType[] methodtypes);

    public delegate void GetEventHandler(string[] apis, string camerastatus, ZoomInfo zoomInfo, bool liveview, StrStrArray postviewSize, IntIntArray selfTimer, StrStrArray shootMode);
}
