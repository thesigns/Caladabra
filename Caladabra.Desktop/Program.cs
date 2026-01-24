using System.Runtime.InteropServices;
using Caladabra.Desktop.Core;

namespace Caladabra.Desktop;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    static void Main(string[] args)
    {
        try
        {
            var game = new Game();
            game.Run();
        }
        catch (Exception ex)
        {
            var errorMessage = $"{ex.Message}\n\n{ex.StackTrace}";

            // Save to error.log
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "error.log");
                File.WriteAllText(logPath, $"[{DateTime.Now}]\n{ex}\n");
            }
            catch { }

            // Show MessageBox on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MessageBox(IntPtr.Zero, errorMessage, "Caladabra - Error", 0x10); // MB_ICONERROR
            }
            else
            {
                Console.Error.WriteLine($"Error: {ex}");
            }
        }
    }
}
