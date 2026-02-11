namespace RemoteDesktop.Shared.Models;

public class AuthRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
