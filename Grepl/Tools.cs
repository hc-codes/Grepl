using System;
using System.IO;
using WConsole = System.Console;

namespace Grepl;

static class Tools
{
    public static TextWriter Console = WConsole.Out;
    // public static TextReader StdInput = WConsole.In;

    public static IDisposable Color(ConsoleColor color)
    {
        return new ColorScope(color);
    }

    class ColorScope : IDisposable
    {
        private readonly ConsoleColor _original;

        public ColorScope(ConsoleColor color)
        {
            _original = WConsole.ForegroundColor;
            WConsole.ForegroundColor = color;
        }

        public void Dispose()
        {
            WConsole.ForegroundColor = _original;
        }
    }
}
