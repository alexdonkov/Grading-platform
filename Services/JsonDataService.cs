using System.Text;
using System.Text.Json;
using EduTrack.Models;

namespace EduTrack.Services;

// Роля 3: единственото място, където четем и записваме файлове.
public class JsonDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string DataDirectory { get; }
    public string SavePath => Path.Combine(DataDirectory, "school-data.json");
    public string DemoPath => Path.Combine(DataDirectory, "demo-data.json");

    // baseDirectory е удобен за отделни тестове, без да се пипат истинските данни.
    public JsonDataService(string? baseDirectory = null)
    {
        DataDirectory = Path.Combine(baseDirectory ?? FindApplicationRoot(), "Data");
    }

    public SchoolData Load(out string? warning)
    {
        warning = null;
        if (File.Exists(SavePath))
        {
            try { return ReadAndValidate(SavePath); }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
            {
                string message = $"Файлът с данните не може да се зареди: {ex.Message}";
                try
                {
                    string suffix = Guid.NewGuid().ToString("N")[..6];
                    string corruptPath = Path.Combine(DataDirectory,
                        $"school-data.corrupt-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{suffix}.json");
                    File.Move(SavePath, corruptPath);
                    message += $" Повреденият файл е запазен като {Path.GetFileName(corruptPath)}.";
                }
                catch (IOException) { message += " Неуспешно отделяне на повредения файл."; }
                catch (UnauthorizedAccessException) { message += " Няма права за преместване на повредения файл."; }

                var restored = TryRestoreBackup(out string? recoveryDetails);
                if (restored is not null)
                {
                    warning = message + " Възстановени са данни от резервното копие. " + recoveryDetails;
                    return restored;
                }
                warning = message + " Няма валидно резервно копие. Зареждат се демонстрационните данни, ако са налични.";
            }
        }
        else if (File.Exists(SavePath + ".bak"))
        {
            // Ако основният файл е изчезнал (напр. след повреда), никога не
            // прескачаме валидното резервно копие, за да заредим старото demo.
            var restored = TryRestoreBackup(out string? recoveryDetails);
            if (restored is not null)
            {
                warning = "Основният JSON файл липсва. Данните са заредени от резервното копие. " + recoveryDetails;
                return restored;
            }
            warning = "Основният JSON файл липсва, а резервното копие е невалидно.";
        }

        if (File.Exists(DemoPath))
        {
            try { return ReadAndValidate(DemoPath); }
            catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
            {
                warning = (warning is null ? "" : warning + " ") +
                    $"Демонстрационните данни не са прочетени ({ex.Message}); стартира празен дневник.";
            }
        }
        else if (warning is null)
        {
            warning = "Няма демонстрационен файл. Стартира празен дневник.";
        }
        return new SchoolData();
    }

    private SchoolData? TryRestoreBackup(out string? details)
    {
        details = null;
        string backupPath = SavePath + ".bak";
        if (!File.Exists(backupPath)) return null;
        try
        {
            SchoolData restored = ReadAndValidate(backupPath);
            if (File.Exists(SavePath))
            {
                // Не презаписваме повреден оригинал, който не е бил отделен.
                details = "Оригиналният файл е запазен; работим с резервните данни.";
            }
            else
            {
                try
                {
                    File.Copy(backupPath, SavePath, overwrite: false);
                    details = "Основният файл е възстановен и за следващото стартиране.";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    details = $"Неуспешно възстановяване на файла ({ex.Message}); резервните данни ще се използват при следващия старт.";
                }
            }
            return restored;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            details = "Резервното копие не може да се прочете.";
            return null;
        }
    }

    public void Save(SchoolData data)
    {
        Validate(data);
        Directory.CreateDirectory(DataDirectory);
        string temporary = SavePath + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            // Запазваме предишната работеща версия; актуализацията става чрез преместване.
            if (File.Exists(SavePath))
            {
                // В резервното копие записваме САМО валиден предходен JSON.
                // Ако основният е повреден, не унищожаваме годния .bak файл.
                try
                {
                    ReadAndValidate(SavePath);
                    File.Copy(SavePath, SavePath + ".bak", overwrite: true);
                }
                catch (Exception ex) when (ex is JsonException or InvalidDataException)
                {
                    // Оставяме старото резервно копие непроменено.
                }
            }
            File.Move(temporary, SavePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static SchoolData ReadAndValidate(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        var data = JsonSerializer.Deserialize<SchoolData>(json)
            ?? throw new InvalidDataException("JSON съдържа null вместо училищни данни.");
        Validate(data);
        data.EnsureCounters();
        return data;
    }

    public static void Validate(SchoolData data)
    {
        if (data.Students is null || data.Grades is null || data.Absences is null
            || data.Subjects is null || data.Classes is null)
            throw new InvalidDataException("Някой от основните списъци в JSON липсва.");
        if (data.Students.Any(s => s is null) || data.Grades.Any(g => g is null)
            || data.Absences.Any(a => a is null) || data.Subjects.Any(s => s is null)
            || data.Classes.Any(c => c is null))
            throw new InvalidDataException("JSON съдържа празен елемент в списък.");
        if (!UniqueIds(data.Students.Select(s => s.Id)) || !UniqueIds(data.Grades.Select(g => g.Id))
            || !UniqueIds(data.Absences.Select(a => a.Id)) || !UniqueIds(data.Subjects.Select(s => s.Id))
            || !UniqueIds(data.Classes.Select(c => c.Id)))
            throw new InvalidDataException("Невалидни или повтарящи се ID номера.");

        var classIds = data.Classes.Select(c => c.Id).ToHashSet();
        var subjectIds = data.Subjects.Select(s => s.Id).ToHashSet();
        var studentIds = data.Students.Select(s => s.Id).ToHashSet();
        if (data.Classes.Any(c => string.IsNullOrWhiteSpace(c.Name)) ||
            data.Subjects.Any(s => string.IsNullOrWhiteSpace(s.Name)))
            throw new InvalidDataException("Празно име на паралелка или предмет.");
        if (data.Classes.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1) ||
            data.Subjects.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new InvalidDataException("Повтарящи се паралелки или предмети.");
        if (data.Students.Any(s => !classIds.Contains(s.ClassId) || s.Number < 1 || s.Number > 40 ||
                string.IsNullOrWhiteSpace(s.FirstName) || string.IsNullOrWhiteSpace(s.LastName)) ||
            data.Students.GroupBy(s => (s.ClassId, s.Number)).Any(g => g.Count() > 1))
            throw new InvalidDataException("Невалидни ученици, номера или липсващи паралелки.");
        if (data.Grades.Any(g => !studentIds.Contains(g.StudentId) || !subjectIds.Contains(g.SubjectId)
                || g.Value < 2 || g.Value > 6 || string.IsNullOrWhiteSpace(g.AssessmentType)))
            throw new InvalidDataException("Оценки с невалидни стойности или липсващи връзки.");
        if (data.Absences.Any(a => !studentIds.Contains(a.StudentId) || !subjectIds.Contains(a.SubjectId)
                || a.Hours < 1 || a.Hours > 8 || a.Date == default))
            throw new InvalidDataException("Отсъствия с невалидни часове или липсващи връзки.");
    }

    private static bool UniqueIds(IEnumerable<int> ids)
    {
        var values = ids.ToList();
        return values.All(id => id > 0) && values.Count == values.Distinct().Count();
    }

    private static string FindApplicationRoot()
    {
        // При F5 Visual Studio стартира от bin/Debug/net8.0.
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "EduTrack.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        return AppContext.BaseDirectory; // При публикувано приложение.
    }
}
