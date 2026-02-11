public class FileChunk
{
    public string FileName { get; set; }
    public int Index { get; set; }        // thứ tự chunk
    public int Total { get; set; }        // tổng số chunk
    public byte[] Data { get; set; }      // dữ liệu chunk
    public string Checksum { get; set; }  // SHA256 của chunk
}
