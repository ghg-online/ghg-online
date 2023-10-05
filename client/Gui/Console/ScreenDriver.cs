using Terminal.ScreenLibrary;
using SysColor = System.Drawing.Color;

namespace client.Gui.Console
{
    public class ScreenDriver : IScreenDriver
    {
        readonly ConsoleScreen consoleScreen;
        readonly int width;
        readonly int height;
        readonly ScreenCell[,] buffer;

        public ScreenDriver(ConsoleScreen consoleScreen)
        {
            this.consoleScreen = consoleScreen;
            width = consoleScreen.DataWidth;
            height = consoleScreen.DataHeight;
            buffer = new ScreenCell[width, height];
        }

        public int Width => width;

        public int Height => height;

        public void Redraw()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    consoleScreen[x, y] = buffer[x, y];
        }

        public void Update(int x, int y, char c, SysColor foreground, SysColor background)
        {
            buffer[x, y].Character = c;
            buffer[x, y].Foreground = foreground;
            buffer[x, y].Background = background;
        }

        public void UpdateCursor(int x, int y, bool show)
        {
            consoleScreen.UpdateCursor(x, y, show);
        }
    }
}
