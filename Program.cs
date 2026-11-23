using System.Text;
using System.Globalization;
using EduTrack.Services;
using EduTrack.UI;

namespace EduTrack;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Полезно при Windows Terminal / Visual Studio с шрифт, поддържащ кирилица.
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("bg-BG");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("bg-BG");
        Console.Title = "EduTrack | Училищна информационна система";

        // Тестовете са отделен вграден режим, без външни NuGet пакети.
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = Tests.SelfTests.Run();
            return;
        }

        var json = new JsonDataService();
        var data = json.Load(out string? warning);
        var register = new SchoolRegister(data);
        var menu = new ConsoleMenu(register, json);
        try { menu.Run(warning); }
        catch (EndOfStreamException)
        {
            // При затворен/пренасочен вход приключваме спокойно.
            Console.WriteLine("\nВходът е прекратен. EduTrack се затваря.");
        }
    }
}
