using SysColor = System.Drawing.Color;

namespace client.Api.Abstraction
{
    public interface IConsole
    {
        char ReadChar();
        string ReadLine();
        void ResetColor();
        void SetBackgroundColor(SysColor color);
        void SetForegroundColor(SysColor color);
        void Write(string message);
        void WriteLine(string message);
    }
}