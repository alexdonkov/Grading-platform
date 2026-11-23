using System.Globalization;

namespace EduTrack.UI;

// Роля 2: само визуализация и четене на валидни данни от конзолата.
public static class ConsoleUi
{
    private const int Width = 79;

    public static void Clear()
    {
        try { if (!Console.IsOutputRedirected) Console.Clear(); }
        catch (IOException) { /* Някои конзоли не позволяват изчистване. */ }
    }

    public static void Header(string title, string subtitle = "")
    {
        Clear();
        if (title == "УЧИЛИЩНО ТАБЛО")
        {
            ColorWriteLine(@"   _____ ____  _   _ _____ ____      _    ____ _  __", ConsoleColor.Cyan);
            ColorWriteLine(@"  | ____|  _ \| | | |_   _|  _ \    / \  / ___| |/ /", ConsoleColor.Cyan);
            ColorWriteLine(@"  |  _| | | | | | | | | | | | |_) |  / _ \| |   | ' /", ConsoleColor.Cyan);
            ColorWriteLine(@"  | |___| |_| | |_| | | | |  _ <  / ___ \ |___| . \", ConsoleColor.Cyan);
            ColorWriteLine(@"  |_____|____/ \___/  |_| |_| \_\/_/   \_\____|_|\_\", ConsoleColor.Cyan);
        }
        Line('═', ConsoleColor.DarkCyan);
        ColorWrite("  EDUTRACK", ConsoleColor.Cyan);
        ColorWriteLine("   /   УЧИЛИЩНА ИНФОРМАЦИОННА СИСТЕМА", ConsoleColor.Gray);
        Line('─', ConsoleColor.DarkCyan);
        ColorWriteLine($"  {title}", ConsoleColor.White);
        if (subtitle.Length > 0) ColorWriteLine($"  {subtitle}", ConsoleColor.DarkGray);
        Line('═', ConsoleColor.DarkCyan);
        Console.WriteLine();
    }

    public static void Section(string title)
    {
        Console.WriteLine();
        ColorWriteLine("  » " + title, ConsoleColor.Cyan);
        Console.WriteLine("  " + new string('─', Math.Min(Width - 2, 72)));
    }

    public static void Option(int number, string text)
    {
        ColorWrite($"  [{number}] ", number == 0 ? ConsoleColor.DarkGray : ConsoleColor.Cyan);
        Console.WriteLine(text);
    }

    public static void DashboardMetric(string label, string value)
    {
        ColorWrite("  " + label.PadRight(30), ConsoleColor.Gray);
        ColorWriteLine(value, ConsoleColor.White);
    }

    public static void Info(string message) => Status("ИНФО", message, ConsoleColor.Cyan);
    public static void Success(string message) => Status("ГОТОВО", message, ConsoleColor.Green);
    public static void Warning(string message) => Status("ВНИМАНИЕ", message, ConsoleColor.Yellow);
    public static void Error(string message) => Status("ГРЕШКА", message, ConsoleColor.Red);

    private static void Status(string tag, string message, ConsoleColor color)
    {
        Console.WriteLine();
        ColorWrite($"  [{tag}] ", color);
        Console.WriteLine(message);
    }

    public static void Table((string Name, int Width)[] columns, IEnumerable<string[]> rows)
    {
        var data = rows.ToList();
        if (data.Count == 0) { ColorWriteLine("  Няма записи за показване.", ConsoleColor.DarkGray); return; }

        // Адаптираме ширините, така че таблиците с оценки и отсъствия
        // да не се отрязват в стандартна конзола с 80 колони.
        int availableWidth = Width;
        try
        {
            if (!Console.IsOutputRedirected && Console.WindowWidth > 35)
                availableWidth = Math.Min(Width, Console.WindowWidth - 1);
        }
        catch (IOException) { /* При конзола без WindowWidth ползваме 79. */ }

        int[] widths = columns.Select(c => c.Width).ToArray();
        int[] minimums = columns.Select(c => c.Width <= 5 ? c.Width :
            c.Name == "Дата" ? 10 : Math.Min(c.Width, 7)).ToArray();
        int maxCellWidth = Math.Max(minimums.Sum(), availableWidth - (3 * columns.Length + 3));
        while (widths.Sum() > maxCellWidth)
        {
            var candidates = Enumerable.Range(0, widths.Length)
                .Where(i => widths[i] > minimums[i])
                .OrderByDescending(i => widths[i] - minimums[i]).ToList();
            if (candidates.Count == 0) break;
            widths[candidates[0]]--;
        }
        var fittedColumns = columns.Select((c, i) => (c.Name, Width: widths[i])).ToArray();

        static string Separator((string Name, int Width)[] cols, string start, string between, string end) =>
            "  " + start + string.Join(between, cols.Select(c => new string('─', c.Width + 2))) + end;
        Console.WriteLine(Separator(fittedColumns, "┌", "┬", "┐"));
        PrintTableRow(fittedColumns, columns.Select(c => c.Name).ToArray(), ConsoleColor.Cyan);
        Console.WriteLine(Separator(fittedColumns, "├", "┼", "┤"));
        foreach (var row in data) PrintTableRow(fittedColumns, row, ConsoleColor.Gray);
        Console.WriteLine(Separator(fittedColumns, "└", "┴", "┘"));
        ColorWriteLine($"  Общо записи: {data.Count}", ConsoleColor.DarkGray);
    }

    private static void PrintTableRow((string Name, int Width)[] columns, string[] values, ConsoleColor color)
    {
        ColorWrite("  │", ConsoleColor.DarkGray);
        for (int i = 0; i < columns.Length; i++)
        {
            string value = i < values.Length ? values[i] ?? "" : "";
            string cell = Fit(value, columns[i].Width);
            ColorWrite(" " + cell.PadRight(columns[i].Width) + " ", color);
            ColorWrite("│", ConsoleColor.DarkGray);
        }
        Console.WriteLine();
    }

    private static string Fit(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)] + "…";

    public static string Text(string prompt, int maxLength = 55, string? defaultValue = null)
    {
        while (true)
        {
            ColorWrite($"  {prompt}{(defaultValue is null ? "" : $" [{defaultValue}]")}: ", ConsoleColor.White);
            string? input = Console.ReadLine();
            if (input is null) throw new EndOfStreamException("Входът е прекратен.");
            input = input.Trim();
            if (input.Length == 0 && defaultValue is not null) return defaultValue;
            if (input.Length > 0 && input.Length <= maxLength) return input;
            Error($"Въведете текст с дължина от 1 до {maxLength} символа.");
        }
    }

    public static int Number(string prompt, int min, int max)
    {
        while (true)
        {
            ColorWrite($"  {prompt} ({min}-{max}): ", ConsoleColor.White);
            string? input = Console.ReadLine();
            if (input is null) throw new EndOfStreamException("Входът е прекратен.");
            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                && number >= min && number <= max)
                return number;
            Error($"Моля, въведете цяло число от {min} до {max}.");
        }
    }

    public static int NumberOrDefault(string prompt, int min, int max, int current)
    {
        while (true)
        {
            ColorWrite($"  {prompt} ({min}-{max}, Enter = {current}): ", ConsoleColor.White);
            string? input = Console.ReadLine();
            if (input is null) throw new EndOfStreamException("Входът е прекратен.");
            if (string.IsNullOrWhiteSpace(input)) return current;
            if (int.TryParse(input, out int value) && value >= min && value <= max) return value;
            Error($"Въведете цяло число от {min} до {max} или Enter.");
        }
    }

    public static DateTime Date(string prompt)
    {
        while (true)
        {
            ColorWrite($"  {prompt} (дд.мм.гггг): ", ConsoleColor.White);
            string? input = Console.ReadLine();
            if (input is null) throw new EndOfStreamException("Входът е прекратен.");
            if (DateTime.TryParseExact(input.Trim(), new[] { "dd.MM.yyyy", "d.M.yyyy" },
                CultureInfo.GetCultureInfo("bg-BG"), DateTimeStyles.None, out var date))
                return date.Date;
            Error("Невалидна дата. Пример: 18.11.2026");
        }
    }

    public static bool Confirm(string question)
    {
        while (true)
        {
            ColorWrite($"  {question} (да/не): ", ConsoleColor.Yellow);
            string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer is null) throw new EndOfStreamException("Входът е прекратен.");
            if (answer is "да" or "д" or "yes" or "y") return true;
            if (answer is "не" or "н" or "no" or "n") return false;
            Error("Отговорете с 'да' или 'не'.");
        }
    }

    public static void Pause()
    {
        Console.WriteLine();
        ColorWrite("  Натиснете Enter за продължаване...", ConsoleColor.DarkGray);
        Console.ReadLine();
    }

    public static void Line(char c, ConsoleColor color) => ColorWriteLine(new string(c, Width), color);

    public static void ColorWrite(string value, ConsoleColor color)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(value);
        Console.ForegroundColor = old;
    }

    public static void ColorWriteLine(string value, ConsoleColor color)
    {
        ColorWrite(value + Environment.NewLine, color);
    }
}
