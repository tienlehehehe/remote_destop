namespace RemoteDesktop.Shared.Models
{
    public enum InputType { Keyboard, Mouse }

    public class InputEvent
    {
        public InputType Type { get; set; }
        public ushort KeyCode { get; set; }
        public bool IsKeyUp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public uint MouseFlags { get; set; }
    }
}
