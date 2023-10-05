using client.Utils;
using System.Collections;
using SysColor = System.Drawing.Color;

namespace client.Api
{
    public class Console : IConsole
    {
        readonly StreamReader reader;
        readonly StreamWriter writer;

        public Console(Stream stream)
        {
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };
        }

        public void Write(string message)
        {
            writer.Write(message);
        }

        public void WriteLine(string message)
        {
            writer.WriteLine(message);
        }

        /// <summary>
        /// Read a single character from the console.
        /// </summary>
        /// <returns>The character</returns>
        /// <exception cref="OverflowException">
        /// The next character cannot be represented by a 16-bit unsigned integer.
        /// </exception>
        public char ReadChar()
        {
            return Convert.ToChar(reader.Read());
        }

        public string ReadLine()
        {
            var chars = new ArrayList(10);
            // status = 0: yet to start reading
            // status = 1: reading
            // status = 2: finished reading
            int status = 0;
            while (status != 2)
            {
                char c;
                try
                {
                    c = ReadChar();
                }
                catch (OverflowException) { continue; }
                switch (c)
                {
                    case '\b':
                        if (chars.Count > 0)
                        {
                            chars.RemoveAt(chars.Count - 1);
                            writer.Write("\b \b");
                        }
                        break;
                    case '\r':
                        break;
                    case '\n':
                        if (status == 1 || status == 0)
                            status = 2;
                        writer.Write('\n');
                        break;
                    default:
                        if (status == 0)
                            status = 1;
                        chars.Add(c);
                        writer.Write(c);
                        break;
                }
            }
            return new string((char[])chars.ToArray(typeof(char)));
        }

        public void SetForegroundColor(SysColor color)
        {
            writer.Write(color.ToForegroundEscapeCode());
        }

        public void SetBackgroundColor(SysColor color)
        {
            writer.Write(color.ToBackgroundEscapeCode());
        }

        public void ResetColor()
        {
            writer.Write("\x1b[0m");
        }
    }
}
