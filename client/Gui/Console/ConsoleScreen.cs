using client.Utils;
using Terminal.Gui;
using Terminal.ScreenLibrary;

namespace client.Gui.Console
{
    public class ConsoleScreen : View
    {
        public int DataWidth { get; }
        public int DataHeight { get; }

        readonly Label[,] labels;

        public ConsoleScreen(int width, int height, Color background) : base()
        {
            base.Width = width;
            base.Height = height;
            base.CanFocus = true;
            DataWidth = width;
            DataHeight = height - 1;
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
        }

        public ScreenCell this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= DataWidth || y < 0 || y >= DataHeight)
                    throw new System.IndexOutOfRangeException();
                return new ScreenCell()
                {
                    Character = Convert.ToChar(labels[x, y].Text[0]),
                    Foreground = labels[x, y].ColorScheme.Normal.Foreground.ToSystemColor(),
                    Background = labels[x, y].ColorScheme.Normal.Background.ToSystemColor(),
                };
            }
            set
            {
                if (x < 0 || x >= DataWidth || y < 0 || y >= DataHeight)
                    throw new System.IndexOutOfRangeException();
                labels[x, y].Text = Char.ToString(value.Character);
                labels[x, y].ColorScheme.Normal =
                    new(value.Foreground.ToTermColor(), value.Background.ToTermColor());
            }
        }

        private int cursorX, cursorY;
        private bool cursorVisible;

        public void UpdateCursor(int x, int y, bool visible)
        {
            cursorX = x;
            cursorY = y;
            cursorVisible = visible;
            //PositionCursor();
            //Application.Driver.UpdateCursor();
        }

        public override void PositionCursor()
        {
            Move(cursorX, cursorY);
            Application.Driver.SetCursorVisibility(CursorVisibility.Default);
            //Application.Driver.SetCursorVisibility(cursorVisible ? CursorVisibility.Default : CursorVisibility.Invisible);
        }
    }
}
