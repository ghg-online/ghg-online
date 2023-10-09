using client.Utils;
using System;
using System.Runtime.InteropServices;
using Terminal.Gui;
using Terminal.ScreenLibrary;

namespace client.Gui.Console
{
    public class ConsoleScreen : View
    {
        public int DataWidth { get; }
        public int DataHeight { get; }

        //readonly Label[,] labels;
        private readonly IScreenData buffer;

        public ConsoleScreen(int width, int height, Color background) : base()
        {
            base.Width = width;
            base.Height = height;
            base.CanFocus = true;
            DataWidth = width;
            DataHeight = height - 1;
            buffer = new ScreenData(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    buffer[x, y] = new ScreenCell()
                    {
                        Character = ' ',
                        Foreground = System.Drawing.Color.White,
                        Background = background.ToSystemColor(),
                    };
                }
            /*
            labels = new Label[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    labels[x, y] = new Label()
                    {
                        X = x,
                        Y = y,
                        Width = 1,
                        Height = 1,
                        Text = " ",
                        ColorScheme = new()
                        {
                            Normal = new(background),
                        },
                    };
                    Add(labels[x, y]);
                }
            */
        }

        public override void Redraw(Rect bounds)
        {
            for (int y = 0; y < DataHeight; y++)
                for (int x = 0; x < DataWidth; x++)
                    if (/*buffer[x, y].Dirty*/true)
                    {
                        Driver.Move(Frame.X + x, Frame.Y + y);
                        var color = Driver.MakeAttribute(
                            buffer[x, y].Foreground.ToTermColor(),
                            buffer[x, y].Background.ToTermColor());
                        Driver.SetAttribute(color);
                        Driver.AddStr(char.ToString(buffer[x, y].Character));
                        buffer.SetDirty(x, y, false);
                    }
            PositionCursor();
            Driver.Refresh();
        }

        public ScreenCell this[int x, int y]
        {
            get => buffer[x, y];
            set => buffer[x, y] = value;
        }

        private int cursorX, cursorY;
        private bool cursorVisible;

        public void UpdateCursor(int x, int y, bool visible)
        {
            cursorX = x;
            cursorY = y;
            cursorVisible = visible;
            PositionCursor();
        }

        public override void PositionCursor()
        {
            Move(cursorX, cursorY);
            Application.Driver.SetCursorVisibility(cursorVisible ? CursorVisibility.Default : CursorVisibility.Invisible);
        }
    }
}
