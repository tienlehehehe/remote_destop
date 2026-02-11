namespace RemoteDesktop.Shared.Models
{
    public class ServerItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsFolder { get; set; }
        public List<ServerItem> Children { get; set; } = new List<ServerItem>();
    }
}
