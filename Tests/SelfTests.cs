using EduTrack.Models;
using EduTrack.Services;
using EduTrack.UI;
using System.Text.Json;

namespace EduTrack.Tests;

// Стартирайте: dotnet run -- --self-test
// Без xUnit / NuGet - подходящо за учебен проект.
public static class SelfTests
{
    private static int _passed;
    private static int _failed;

    public static int Run()
    {
        _passed = 0; _failed = 0;
        Console.WriteLine("EDUTRACK | АВТОМАТИЧНИ ПРОВЕРКИ");
        CheckTest("Ученици, оценки, статистика, филтри, преместване", TestRegister);
        CheckTest("Прогноза на успеха без запис", TestSimulator);
        CheckTest("EduInsights и постижения", TestInsightsAndAchievements);
        CheckTest("Smart Alerts и отсъствия", TestAlertsAndAbsences);
        CheckTest("Празна база и невалидни операции", TestEmptyData);
        CheckTest("JSON - запазване, зареждане и възстановяване", TestPersistence);
        CheckTest("JSON - целостта на данните и демонстрационни записи", TestDataIntegrity);
        CheckTest("Конзолни менюта - въвеждане, навигация, проверки и запис", TestConsoleMenus);
        CheckTest("Конзолни менюта - празна база без срив", TestEmptyConsoleMenus);
        Console.WriteLine($"\nРЕЗУЛТАТ: {_passed} успешни / {_failed} неуспешни теста.");
        return _failed == 0 ? 0 : 1;
    }

    private static void CheckTest(string name, Action test)
    {
        try { test(); _passed++; Console.WriteLine("[OK] " + name); }
        catch (Exception ex) { _failed++; Console.WriteLine("[FAIL] " + name + ": " + ex); }
    }

    private static void Assert(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Тестът не премина: " + description);
    }
    private static void Approx(double? actual, double expected, string description) =>
        Assert(actual.HasValue && Math.Abs(actual.Value - expected) < 0.0000001, description);

    private static void MustThrow(Action action, string description)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Очаквахме грешка: " + description);
    }

    private static void TestRegister()
    {
        var reg = new SchoolRegister(new SchoolData());
        var c1 = reg.AddClass("10а");
        var c2 = reg.AddClass("10Б");
        var math = reg.AddSubject("Математика");
        var bel = reg.AddSubject("Български език и литература");
        var s = reg.AddStudent("Ева", "Иванова", c1.Id, 1);
        var s2 = reg.AddStudent("Габриела", "Петрова", c1.Id, 2);
        Assert(s.Id != s2.Id && c1.Name == "10А", "IDs и нормализиране на клас");
        MustThrow(() => reg.AddStudent("Друг", "Ученик", c1.Id, 1), "номерът в клас е уникален");
        MustThrow(() => reg.AddClass("10а"), "без дублирани паралелки");
        MustThrow(() => reg.AddSubject("математика"), "без дублирани предмети");
        var g1 = reg.AddGrade(s.Id, math.Id, 4, "Текуща");
        reg.AddGrade(s.Id, math.Id, 6, "Проект");
        reg.AddGrade(s.Id, bel.Id, 5, "Класна");
        var stats = new StatisticsService(reg);
        Approx(stats.StudentAverage(s.Id), 5.0, "средна оценка за ученик");
        Approx(stats.StudentSubjectAverage(s.Id, math.Id), 5.0, "предметна средна");
        Approx(stats.ClassAverage(c1.Id), 5.0, "средна за клас");
        Approx(stats.OverallAverage(), 5.0, "обща средна");
        Assert(stats.GradeDistribution()[4] == 1, "разпределение на оценките");
        Assert(stats.TopStudents(5).Count() == 1, "няма ученици без оценки в TOP");
        reg.EditGrade(g1.Id, math.Id, 5, "Контролна");
        Approx(stats.StudentAverage(s.Id), 16.0 / 3, "редактиране на оценка");
        reg.DeleteGrade(g1.Id);
        Assert(reg.Data.Grades.Count == 2, "изтриване на оценка");
        reg.EditStudent(s.Id, "Ева", "Иванова", c2.Id, 3);
        Assert(reg.GetStudent(s.Id).ClassId == c2.Id && reg.GetStudent(s.Id).Number == 3, "преместване");
        MustThrow(() => reg.DeleteSubject(math.Id), "не трием предмет с оценки");
        MustThrow(() => reg.DeleteClass(c2.Id), "не трием паралелка с ученици");
        reg.DeleteStudent(s.Id);
        Assert(reg.Data.Grades.Count == 0, "каскадно изтриване на оценки");
        reg.DeleteClass(c2.Id);
        reg.DeleteSubject(math.Id);
        Assert(reg.Data.Classes.Count == 1 && reg.Data.Subjects.Count == 1, "изтриване на празни записи");
    }

    private static void TestSimulator()
    {
        var reg = new SchoolRegister(new SchoolData());
        var c = reg.AddClass("10А"); var math = reg.AddSubject("Математика");
        var bel = reg.AddSubject("БЕЛ"); var s = reg.AddStudent("Иван", "Иванов", c.Id, 1);
        reg.AddGrade(s.Id, math.Id, 4, "Текуща");
        reg.AddGrade(s.Id, math.Id, 6, "Проект");
        reg.AddGrade(s.Id, bel.Id, 5, "Класна");
        var stats = new StatisticsService(reg);
        var svc = new InsightService(reg, stats);
        var projected = svc.Simulate(s.Id, math.Id, 6);
        Approx(projected.OldSubject, 5.0, "предишен успех предмет");
        Approx(projected.NewSubject, 16.0 / 3, "нов успех по предмет");
        Approx(projected.OldOverall, 5.0, "предишен общ успех");
        Approx(projected.NewOverall, 21.0 / 4, "нов общ успех");
        Assert(reg.Data.Grades.Count == 3, "симулаторът не записва оценки");
        var s2 = reg.AddStudent("Петър", "Тодоров", c.Id, 2);
        var fresh = svc.Simulate(s2.Id, math.Id, 6);
        Assert(fresh.OldSubject is null && fresh.OldOverall is null, "празен успех е null");
        Approx(fresh.NewSubject, 6, "първа симулирана оценка");
        Approx(fresh.NewOverall, 6, "първа симулирана обща оценка");
    }

    private static void TestInsightsAndAchievements()
    {
        var reg = new SchoolRegister(new SchoolData());
        var c = reg.AddClass("10А"); var m = reg.AddSubject("Математика");
        var b = reg.AddSubject("БЕЛ"); var e = reg.AddSubject("Английски");
        var s = reg.AddStudent("Мария", "Димитрова", c.Id, 1);
        foreach (int n in new[] { 3, 3, 5, 5 }) reg.AddGrade(s.Id, m.Id, n, "Текуща");
        var stats = new StatisticsService(reg);
        var insights = new InsightService(reg, stats);
        Assert(insights.AnalyzeTrend(s.Id).Direction == Trend.Improving, "повишение на последните 2");
        Assert(insights.AnalyzeTrend(s.Id).Difference == 2.0, "разликата е 2.00");
        var report = insights.GetReport(s.Id);
        Assert(report.StrongestSubject.Contains("Математика"), "сила по предмет");
        var achievements = new AchievementService(reg, stats, insights);
        Assert(achievements.ForStudent(s.Id).Any(x => x.StartsWith("Напредък")), "награда за напредък");
        foreach (int n in new[] { 6, 6 }) reg.AddGrade(s.Id, b.Id, n, "Текуща");
        foreach (int n in new[] { 6, 6 }) reg.AddGrade(s.Id, e.Id, n, "Текуща");
        Assert(achievements.ForStudent(s.Id).Any(x => x.StartsWith("Постоянство")), "постоянство 8 оценки / 3 предмета");
        var s2 = reg.AddStudent("Никола", "Георгиев", c.Id, 2);
        foreach (int n in new[] { 6, 6, 6, 6, 6 }) reg.AddGrade(s2.Id, m.Id, n, "Текуща");
        Assert(achievements.ForStudent(s2.Id).Any(x => x.StartsWith("Отличен")), "отличен успех с 5 оценки");
        Assert(achievements.ForStudent(s2.Id).Any(x => x.StartsWith("Отговорност")),
            "ученик без нито едно отсъствие също е без неизвинени");
    }

    private static void TestAlertsAndAbsences()
    {
        var reg = new SchoolRegister(new SchoolData());
        var c = reg.AddClass("10А"); var sub = reg.AddSubject("История");
        var s = reg.AddStudent("Александър", "Павлов", c.Id, 1);
        foreach (int n in new[] { 5, 6, 2, 2 }) reg.AddGrade(s.Id, sub.Id, n, "Текуща");
        var stats = new StatisticsService(reg);
        var insights = new InsightService(reg, stats);
        var alerts = new AlertService(reg, stats, insights);
        Assert(alerts.GetAlerts().Any(x => x.Message.Contains("спад")), "значителен спад в оценките");
        var absence = reg.AddAbsence(s.Id, sub.Id, DateTime.Today, 3);
        Assert(stats.AbsenceHours(s.Id, excused: false) == 3, "неизвинени часове");
        Assert(alerts.GetAlerts().Any(x => x.Message.Contains("неизвинени")), "предупреждение за отсъствия");
        reg.SetAbsenceExcused(absence.Id, true, "Медицинска бележка");
        Assert(stats.AbsenceHours(s.Id, excused: false) == 0, "извинението променя статистиката");
        Assert(!alerts.GetAlerts().Any(x => x.Message.Contains("неизвинени")), "известие изчезва след извинение");
        var achievements = new AchievementService(reg, stats, insights);
        Assert(achievements.ForStudent(s.Id).Any(x => x.StartsWith("Отговорност")), "постижение без неизвинени");
        reg.SetAbsenceExcused(absence.Id, false);
        Assert(achievements.ForStudent(s.Id).All(x => !x.StartsWith("Отговорност")), "отмяна на постижение");
        reg.DeleteStudent(s.Id);
        Assert(reg.Data.Absences.Count == 0, "каскадно изтриване на отсъствия");
    }

    private static void TestEmptyData()
    {
        var reg = new SchoolRegister(new SchoolData());
        var stats = new StatisticsService(reg);
        var ins = new InsightService(reg, stats);
        Assert(stats.OverallAverage() is null && !stats.TopStudents().Any(), "няма делене на нула");
        Assert(stats.AbsenceHours() == 0, "нулеви отсъствия");
        Assert(!new AlertService(reg, stats, ins).GetAlerts().Any(), "няма фалшиви известия");
        MustThrow(() => reg.AddStudent("Тест", "Ученик", 99, 1), "несъществуващ клас");
        var c = reg.AddClass("9А"); var sub = reg.AddSubject("Математика");
        var s = reg.AddStudent("Тест", "Ученик", c.Id, 1);
        MustThrow(() => reg.AddGrade(s.Id, sub.Id, 7, "Текуща"), "невалидна оценка");
        MustThrow(() => reg.AddAbsence(s.Id, sub.Id, DateTime.Today.AddDays(1), 1), "бъдеща дата");
        MustThrow(() => reg.AddAbsence(s.Id, sub.Id, DateTime.Today, 9), "твърде много часове");
    }

    private static void TestPersistence()
    {
        string root = Path.Combine(Path.GetTempPath(), "edutrack-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var json = new JsonDataService(root);
            var empty = json.Load(out _);
            Assert(empty.Students.Count == 0, "нова инсталация без demo");
            var register = new SchoolRegister(empty);
            var c = register.AddClass("10А"); var subject = register.AddSubject("Математика");
            var s = register.AddStudent("Проба", "Тестова", c.Id, 1);
            register.AddGrade(s.Id, subject.Id, 6, "Проект");
            register.AddAbsence(s.Id, subject.Id, DateTime.Today, 2);
            json.Save(register.Data);
            Assert(File.Exists(json.SavePath), "файлът е записан");
            var loaded = json.Load(out var warning);
            Assert(warning is null && loaded.Students.Count == 1 && loaded.Grades.Count == 1
                && loaded.Absences.Count == 1, "зареждане след рестарт");
            register.AddGrade(s.Id, subject.Id, 5, "Текуща");
            json.Save(register.Data);
            Assert(File.Exists(json.SavePath + ".bak"), "има резервно копие");
            File.WriteAllText(json.SavePath, "{ повреден JSON");
            var restored = json.Load(out var problem);
            Assert(problem is not null && restored.Grades.Count == 1, "възстановяване от backup");
            Assert(File.Exists(json.SavePath), "възстановеният файл вече е основен JSON");
            var restarted = json.Load(out _);
            Assert(restarted.Grades.Count == 1, "данните остават и при второ стартиране");
            File.Delete(json.SavePath);
            var missingPrimary = json.Load(out var missingWarning);
            Assert(missingPrimary.Grades.Count == 1 && missingWarning is not null,
                "резервният файл се ползва дори при липсващ основен JSON");
            Assert(File.Exists(json.SavePath), "липсващият JSON е възстановен");
            Assert(Directory.GetFiles(json.DataDirectory, "*.corrupt-*.json").Length == 1,
                "повреденият файл е запазен");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void TestDataIntegrity()
    {
        string root = Path.Combine(Path.GetTempPath(), "edutrack-integrity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new JsonDataService(root);
            var reg = new SchoolRegister(new SchoolData());
            var class10 = reg.AddClass("10А");
            var math = reg.AddSubject("Математика");
            var student = reg.AddStudent("Иван", "Иванов", class10.Id, 1);
            reg.AddGrade(student.Id, math.Id, 5, "Текуща");
            Directory.CreateDirectory(service.DataDirectory);
            File.WriteAllText(service.DemoPath, JsonSerializer.Serialize(reg.Data));
            var first = service.Load(out _);
            Assert(first.Students.Count == 1 && !File.Exists(service.SavePath),
                "демонстрационният файл се чете, без да се презаписва");
            var active = new SchoolRegister(first);
            active.AddGrade(student.Id, math.Id, 6, "Проект");
            service.Save(active.Data);
            var second = service.Load(out _);
            Assert(second.Grades.Count == 2, "потребителските данни имат приоритет пред демонстрацията");
            second.Grades[0].StudentId = 9999;
            try
            {
                JsonDataService.Validate(second);
                throw new Exception("Очакваната проверка за нарушени връзки липсва.");
            }
            catch (InvalidDataException) { /* връзката е правилно отказана */ }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void TestConsoleMenus()
    {
        string root = Path.Combine(Path.GetTempPath(), "edutrack-ui-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new JsonDataService(root);
            var reg = new SchoolRegister(new SchoolData());
            string[] keys =
            {
                "5", "2", "10А", "", "0",                       // паралелка + назад
                "4", "2", "Математика", "", "0",              // предмет + назад
                "1", "2", "1", "Ева", "Иванова", "1", "",   // нов ученик
                "3", "1", "Ева", "", "0",                    // търсене по име
                "0",                                              // от ученици
                "2", "2", "1", "1", "9", "6", "1", "",     // оценка; 9 е невалидна
                "5", "1", "1", "2", "",                   // филтър/сортиране
                "0",                                              // от оценки
                "3", "2", "1", "1", DateTime.Today.ToString("dd.MM.yyyy"), "1", "", // отсъствие
                "3", "1", "да", "Медицинска бележка", "",  // извиняване
                "0",                                              // от отсъствия
                "6", "1", "", "0",                          // статистика
                "7", "2", "1", "1", "5", "", "0",          // симулатор и назад
                "8", "",                                        // известия
                "0"                                               // изход
            };
            string transcript = RunConsoleScript(reg, service, keys);
            Assert(reg.Data.Students.Count == 1 && reg.Data.Grades.Count == 1
                && reg.Data.Absences.Count == 1, "менютата създават реални записи");
            Assert(reg.Data.Grades[0].Value == 6, "невалидна оценка е отхвърлена");
            Assert(reg.Data.Absences[0].IsExcused, "извиняването работи от менюто");
            Assert(transcript.Contains("Моля, въведете цяло число", StringComparison.Ordinal),
                "проверка за невалиден числов вход");
            Assert(transcript.Contains("Това е симулация", StringComparison.Ordinal),
                "симулаторът се отваря");
            Assert(File.Exists(service.SavePath), "данните се запазват при промяна от менюто");
            var reloaded = service.Load(out _);
            Assert(reloaded.Grades.Count == 1 && reloaded.Absences[0].IsExcused,
                "данните остават след рестарт");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void TestEmptyConsoleMenus()
    {
        string root = Path.Combine(Path.GetTempPath(), "edutrack-empty-ui-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new JsonDataService(root);
            var register = new SchoolRegister(new SchoolData());
            // Разглеждаме всички главни менюта; празните справки не трябва да хвърлят грешки.
            string[] keys =
            {
                "1", "1", "", "0", // ученици
                "2", "1", "", "0", // оценки
                "3", "1", "", "0", // отсъствия
                "4", "1", "", "0", // предмети
                "5", "1", "", "0", // класове
                "6", "2", "", "0", // класация
                "7", "1", "", "0", // персонален анализ без ученици
                "8", "",           // предупреждения
                "0"
            };
            string transcript = RunConsoleScript(register, service, keys);
            Assert(transcript.Contains("Няма записи", StringComparison.Ordinal),
                "празните таблици се показват коректно");
            Assert(register.Data.Students.Count == 0 && !File.Exists(service.SavePath),
                "разглеждане на менюта не създава излишни записи");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string RunConsoleScript(SchoolRegister register, JsonDataService service, string[] inputs)
    {
        TextReader originalInput = Console.In;
        TextWriter originalOutput = Console.Out;
        var output = new StringWriter();
        try
        {
            Console.SetIn(new StringReader(string.Join(Environment.NewLine, inputs) + Environment.NewLine));
            Console.SetOut(output);
            new ConsoleMenu(register, service).Run(null);
            return output.ToString();
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
            output.Dispose();
        }
    }

}
