namespace RemoteDesktop.Shared.Models
{
    public class FramePacket
    {
        public int Sequence { get; set; }
        public long TimestampUtc { get; set; }
        public byte[] JpegBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
