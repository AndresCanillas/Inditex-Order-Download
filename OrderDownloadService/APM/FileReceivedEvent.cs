using Service.Contracts;

namespace OrderDonwLoadService
{
    public enum FileType
    {
        Json,
        JsonColor
    }
    public class FileReceivedEvent : EQEventInfo
    {
        public string FilePath { get; set; }
        public string OrderNumber { get; set; }
        public string ProyectId { get; set; }
        public string PluginType { get; set; }
    }
}