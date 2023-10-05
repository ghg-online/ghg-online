using TermColor = Terminal.Gui.Color;
using SysColor = System.Drawing.Color;

namespace client.Utils
{
    public static class ColorExtensions
    {
        static bool initialized = false;
        static readonly IDictionary<TermColor, SysColor> termToSys
            = new Dictionary<TermColor, SysColor>();
        /*
        static readonly IDictionary<ConsoleColor,SysColor> consoleToSys
            = new Dictionary<ConsoleColor, SysColor>();
        */
        static readonly IDictionary<SysColor, TermColor> sysToTermCache
            = new Dictionary<SysColor, TermColor>();

        /// <remarks>
        /// Although System.Drawing.Color contains a value Alpha representing
        /// transparency, we assume that all the SysColors in the following
        /// array has no transparency, which means Alpha == 0xFF
        /// This assumption is used in <see cref="ToTermColor(SysColor)"/>
        /// </remarks>
        static readonly (TermColor, SysColor)[] termColors = new[]
        {
            (TermColor.Black, SysColor.Black),
            (TermColor.Blue, SysColor.Blue),
            (TermColor.Green, SysColor.Green),
            (TermColor.Cyan, SysColor.Cyan),
            (TermColor.Red, SysColor.Red),
            (TermColor.Magenta, SysColor.Magenta),
            (TermColor.Brown, SysColor.Brown),
            (TermColor.DarkGray, SysColor.DarkGray),
            (TermColor.Gray, SysColor.Gray),
            (TermColor.White, SysColor.White),
            (TermColor.BrightCyan, SysColor.LightCyan),
            (TermColor.BrightBlue, SysColor.LightBlue),
            (TermColor.BrightGreen, SysColor.LightGreen),
            (TermColor.BrightMagenta, SysColor.LightPink),
            (TermColor.BrightRed, SysColor.FromArgb(0xff, 0xff, 0x00, 0x0d)),
            (TermColor.BrightYellow, SysColor.Yellow),
        };

        /*
        static readonly (ConsoleColor, SysColor)[] consoleColors = new[]
        {
            (ConsoleColor.Black, SysColor.Black),
            (ConsoleColor.DarkBlue, SysColor.DarkBlue),
            (ConsoleColor.DarkGreen, SysColor.DarkGreen),
            (ConsoleColor.DarkCyan, SysColor.DarkCyan),
            (ConsoleColor.DarkRed, SysColor.DarkRed),
            (ConsoleColor.DarkMagenta, SysColor.DarkMagenta),
            (ConsoleColor.DarkYellow, SysColor.Yellow),
            (ConsoleColor.Gray, SysColor.Gray),
            (ConsoleColor.DarkGray, SysColor.DarkGray),
            (ConsoleColor.Blue, SysColor.Blue),
            (ConsoleColor.Green, SysColor.Green),
            (ConsoleColor.Cyan, SysColor.Cyan),
            (ConsoleColor.Red, SysColor.Red),
            (ConsoleColor.Magenta, SysColor.Magenta),
            (ConsoleColor.Yellow, SysColor.LightYellow),
            (ConsoleColor.White, SysColor.White),
        };
        */

        static void EnsureInitialized()
        {
            if (initialized == false)
            {
                initialized = true;
                foreach (var pair in termColors)
                    termToSys.Add(pair.Item1, pair.Item2);
                /*
                foreach (var pair in consoleColors)
                    consoleToSys.Add(pair.Item1, pair.Item2);
                */
            }
        }

        public static SysColor ToSystemColor(this TermColor termColor)
        {
            EnsureInitialized();
            return termToSys[termColor];
        }

        public static TermColor ToTermColor(this SysColor sysColor)
        {
            EnsureInitialized();
            try
            {
                return sysToTermCache[sysColor];
            }
            catch (KeyNotFoundException)
            {
                // Calculate distances
                int length = termColors.Length;
                double[] distances = new double[length];
                for (int i = 0; i < length; i++)
                    distances[i] = ColorDistance(termColors[i].Item2, sysColor);

                // Find the index of the smallest distance
                double smallest = distances[0]; // Assume termColors.Length > 0
                int indexOfSmallest = 0;
                for (int i = 1; i < length; i++)
                {
                    double value = distances[i];
                    if (value < smallest)
                    {
                        smallest = value;
                        indexOfSmallest = i;
                    }
                }

                // Get the color whose distance is the smallest
                TermColor termColor = termColors[indexOfSmallest].Item1;

                // Add it to cache
                sysToTermCache.Add(sysColor, termColor);

                // Return value
                return termColor;
            }

            // We assume that a.A == 0xFF and b.A == 0xFF
            static double ColorDistance(SysColor a, SysColor b)
            {
                int deltaR = a.R - b.R;
                int deltaG = a.G - b.G;
                int deltaB = a.B - b.B;
                return Math.Sqrt(Math.Pow(deltaR, 2) + Math.Pow(deltaG, 2) + Math.Pow(deltaB, 2));
            }
        }

        public static string ToForegroundEscapeCode(this SysColor color)
        {
            return $"\x1b[38;2;{color.R};{color.G};{color.B}m";
        }

        public static string ToBackgroundEscapeCode(this SysColor color)
        {
            return $"\x1b[48;2;{color.R};{color.G};{color.B}m";
        }

        public static string ToForegroundEscapeCode(this TermColor color)
        {
            return color.ToSystemColor().ToForegroundEscapeCode();
        }

        public static string ToBackgroundEscapeCode(this TermColor color)
        {
            return color.ToSystemColor().ToBackgroundEscapeCode();
        }
    }
}
