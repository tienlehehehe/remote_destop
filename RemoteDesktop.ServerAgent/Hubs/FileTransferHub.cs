using Microsoft.AspNetCore.SignalR;
using RemoteDesktop.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class FileTransferHub : Hub
{
    private readonly string _uploadFolder;

    public FileTransferHub()
    {
        // Thống nhất thư mục lưu file
        _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_uploadFolder);
    }

    // Lấy danh sách ổ đĩa trên server
    public Task<List<ServerItem>> GetServerDrives()
    {
        var drives = new List<ServerItem>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                drives.Add(new ServerItem
                {
                    Name = drive.Name,
                    FullPath = drive.RootDirectory.FullName,
                    IsFolder = true
                });
            }
        }
        return Task.FromResult(drives);
    }

    // Lấy cây thư mục/file từ một path
    public Task<ServerItem> GetServerTree(string path)
    {
        var root = new ServerItem
        {
            Name = Path.GetFileName(path) == string.Empty ? path : Path.GetFileName(path),
            FullPath = path,
            IsFolder = Directory.Exists(path)
        };

        try
        {
            if (Directory.Exists(path))
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    root.Children.Add(new ServerItem
                    {
                        Name = Path.GetFileName(dir),
                        FullPath = dir,
                        IsFolder = true
                    });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    root.Children.Add(new ServerItem
                    {
                        Name = Path.GetFileName(file),
                        FullPath = file,
                        IsFolder = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetServerTree error: " + ex.Message);
        }

        return Task.FromResult(root);
    }

    // Upload nguyên file (cách cũ)
    public async Task<bool> UploadFile(string fileName, byte[] data)
    {
        try
        {
            var path = Path.Combine(_uploadFolder, fileName);
            await File.WriteAllBytesAsync(path, data);

            Console.WriteLine($"[UploadFile] {fileName} saved to {path}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("UploadFile error: " + ex.Message);
            return false;
        }
    }

    // Download nguyên file (cách cũ)
    public async Task<byte[]> DownloadFile(string fileName)
    {
        try
        {
            var path = Path.Combine(_uploadFolder, fileName);
            if (File.Exists(path))
                return await File.ReadAllBytesAsync(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine("DownloadFile error: " + ex.Message);
        }
        return null;
    }

    // Upload theo chunk
    public async Task UploadChunk(string fileName, byte[] chunk, int offset)
    {
        try
        {
            var path = Path.Combine(_uploadFolder, fileName);

            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                await fs.WriteAsync(chunk, 0, chunk.Length);
            }

            Console.WriteLine($"[UploadChunk] {fileName} offset={offset} size={chunk.Length}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("UploadChunk error: " + ex.Message);
            throw;
        }
    }

    // Download theo chunk
    public async Task<byte[]> DownloadChunk(string filePath, int offset, int size)
    {
        try
        {
            if (!File.Exists(filePath))
                return Array.Empty<byte>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                var buffer = new byte[size];
                var read = await fs.ReadAsync(buffer, 0, size);

                if (read < size)
                {
                    Array.Resize(ref buffer, read);
                }

                Console.WriteLine($"[DownloadChunk] {filePath} offset={offset} size={read}");
                return buffer;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("DownloadChunk error: " + ex.Message);
            throw;
        }
    }

    // Lấy kích thước file để client tính % tiến trình
    public Task<long> GetFileSize(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var length = new FileInfo(filePath).Length;
                return Task.FromResult(length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetFileSize error: " + ex.Message);
        }
        return Task.FromResult(0L);
    }

    public Task CompleteUpload(string fileName)
    {
        try
        {
            var path = Path.Combine(_uploadFolder, fileName);
            if (File.Exists(path))
            {
                var length = new FileInfo(path).Length;
                Console.WriteLine($"[CompleteUpload] {fileName} finished. Size={length} bytes");
            }
            else
            {
                Console.WriteLine($"[CompleteUpload] {fileName} not found in uploads folder.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("CompleteUpload error: " + ex.Message);
        }

        return Task.CompletedTask;
    }
}
