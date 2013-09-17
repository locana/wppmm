
namespace WPPMM.Json
{
    public delegate void MethodTypesHandler(string name, string[] reqtypes, string[] restypes, string version);

    public delegate void GetEventHandler(string[] apis, string camerastatus, ZoomInfo zoomInfo, bool liveview, StrStrArray postviewSize, IntIntArray selfTimer, StrStrArray shootMode);
}
