using EduTrack.Models;
using EduTrack.Services;

namespace EduTrack.UI;

public partial class ConsoleMenu
{
    private readonly SchoolRegister _register;
    private readonly JsonDataService _json;
    private readonly StatisticsService _stats;
    private readonly InsightService _insights;
    private readonly AchievementService _achievements;
    private readonly AlertService _alerts;
    private SchoolData Data => _register.Data;

    public ConsoleMenu(SchoolRegister register, JsonDataService json)
    {
        _register = register;
        _json = json;
        _stats = new StatisticsService(register);
        _insights = new InsightService(register, _stats);
        _achievements = new AchievementService(register, _stats, _insights);
        _alerts = new AlertService(register, _stats, _insights);
    }

    public void Run(string? startupWarning)
    {
        if (!string.IsNullOrWhiteSpace(startupWarning))
        {
            ConsoleUi.Header("Зареждане на данните");
            ConsoleUi.Warning(startupWarning);
            ConsoleUi.Pause();
        }
        while (true)
        {
            Dashboard();
            ConsoleUi.Section("ОСНОВНО МЕНЮ");
            ConsoleUi.Option(1, "Ученици и профили");
            ConsoleUi.Option(2, "Оценки и успех");
            ConsoleUi.Option(3, "Отсъствия");
            ConsoleUi.Option(4, "Учебни предмети");
            ConsoleUi.Option(5, "Паралелки");
            ConsoleUi.Option(6, "Статистика и класации");
            ConsoleUi.Option(7, "EduInsights / Симулатор / Постижения");
            ConsoleUi.Option(8, "Автоматични известия (Smart Alerts)");
            ConsoleUi.Option(0, "Изход");
            int choice = ConsoleUi.Number("Избор", 0, 8);
            switch (choice)
            {
                case 0: ConsoleUi.Success("Работата с EduTrack приключи."); return;
                case 1: StudentsMenu(); break;
                case 2: GradesMenu(); break;
                case 3: AbsencesMenu(); break;
                case 4: SubjectsMenu(); break;
                case 5: ClassesMenu(); break;
                case 6: StatisticsMenu(); break;
                case 7: ToolsMenu(); break;
                case 8: AlertsScreen(); break;
            }
        }
    }

    private void Dashboard()
    {
        ConsoleUi.Header("УЧИЛИЩНО ТАБЛО", "Обобщен преглед на училищната дейност");
        var best = _stats.ClassRanking().FirstOrDefault(c => c.Average.HasValue);
        ConsoleUi.DashboardMetric("Ученици", Data.Students.Count.ToString());
        ConsoleUi.DashboardMetric("Общ среден успех", StatisticsService.Format(_stats.OverallAverage()));
        ConsoleUi.DashboardMetric("Отсъствия (учебни часове)", _stats.AbsenceHours().ToString());
        ConsoleUi.DashboardMetric("Най-успешна паралелка",
            best.Class is null ? "няма данни" : $"{best.Class.Name}  /  {StatisticsService.Format(best.Average)}");
        var alerts = _alerts.GetAlerts();
        ConsoleUi.DashboardMetric("Активни предупреждения", alerts.Count.ToString());
        ConsoleUi.Section("ПОСЛЕДНИ ОЦЕНКИ");
        PrintGrades(Data.Grades.OrderByDescending(g => g.Date).ThenByDescending(g => g.Id).Take(4));
        if (alerts.Count > 0)
        {
            ConsoleUi.Section("ВАЖНИ ИЗВЕСТИЯ");
            foreach (var alert in alerts.Take(3))
                ConsoleUi.Warning($"{alert.StudentName}: {alert.Message}");
            if (alerts.Count > 3) ConsoleUi.Info($"Още {alerts.Count - 3} известия в модул Smart Alerts.");
        }
        else
        {
            ConsoleUi.Info("Няма активни предупреждения по зададените прагове.");
        }
    }

    private void Change(Action action, string message)
    {
        try
        {
            action();
            try
            {
                _json.Save(Data);
                ConsoleUi.Success(message + " Промените са запазени в JSON.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                ConsoleUi.Error($"Промяната е в паметта, но НЕ е записана във файл: {ex.Message}");
                ConsoleUi.Warning("Не затваряйте приложението, преди да възстановите достъпа за запис.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ConsoleUi.Error(ex.Message);
        }
        ConsoleUi.Pause();
    }

    private Student? SelectStudent()
    {
        ConsoleUi.Section("ИЗБОР НА УЧЕНИК");
        PrintStudents(_register.StudentsSorted());
        if (Data.Students.Count == 0) { ConsoleUi.Pause(); return null; }
        while (true)
        {
            int id = ConsoleUi.Number("ID на ученик (0 = назад)", 0, 1000000);
            if (id == 0) return null;
            var item = Data.Students.FirstOrDefault(s => s.Id == id);
            if (item is not null) return item;
            ConsoleUi.Error("Няма ученик с такъв ID.");
        }
    }

    private Subject? SelectSubject()
    {
        ConsoleUi.Section("ИЗБОР НА ПРЕДМЕТ");
        PrintSubjects();
        if (Data.Subjects.Count == 0) { ConsoleUi.Pause(); return null; }
        while (true)
        {
            int id = ConsoleUi.Number("ID на предмет (0 = назад)", 0, 1000000);
            if (id == 0) return null;
            var item = Data.Subjects.FirstOrDefault(s => s.Id == id);
            if (item is not null) return item;
            ConsoleUi.Error("Няма предмет с такъв ID.");
        }
    }

    private SchoolClass? SelectClass()
    {
        ConsoleUi.Section("ИЗБОР НА ПАРАЛЕЛКА");
        PrintClasses();
        if (Data.Classes.Count == 0) { ConsoleUi.Pause(); return null; }
        while (true)
        {
            int id = ConsoleUi.Number("ID на паралелка (0 = назад)", 0, 1000000);
            if (id == 0) return null;
            var item = Data.Classes.FirstOrDefault(c => c.Id == id);
            if (item is not null) return item;
            ConsoleUi.Error("Няма паралелка с такъв ID.");
        }
    }

    private Grade? SelectGrade()
    {
        ConsoleUi.Section("ИЗБОР НА ОЦЕНКА");
        PrintGrades(Data.Grades.OrderByDescending(g => g.Date).ThenByDescending(g => g.Id));
        if (Data.Grades.Count == 0) { ConsoleUi.Pause(); return null; }
        while (true)
        {
            int id = ConsoleUi.Number("ID на оценка (0 = назад)", 0, 1000000);
            if (id == 0) return null;
            var item = Data.Grades.FirstOrDefault(g => g.Id == id);
            if (item is not null) return item;
            ConsoleUi.Error("Няма оценка с такъв ID.");
        }
    }

    private Absence? SelectAbsence()
    {
        ConsoleUi.Section("ИЗБОР НА ОТСЪСТВИЕ");
        PrintAbsences(Data.Absences.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id));
        if (Data.Absences.Count == 0) { ConsoleUi.Pause(); return null; }
        while (true)
        {
            int id = ConsoleUi.Number("ID на отсъствие (0 = назад)", 0, 1000000);
            if (id == 0) return null;
            var item = Data.Absences.FirstOrDefault(a => a.Id == id);
            if (item is not null) return item;
            ConsoleUi.Error("Няма отсъствие с такъв ID.");
        }
    }

    private void PrintStudents(IEnumerable<Student> students) => ConsoleUi.Table(
        new[] { ("ID", 4), ("№", 3), ("Ученик", 29), ("Клас", 7), ("Среден", 8) },
        students.Select(s => new[] { s.Id.ToString(), s.Number.ToString(), s.FullName,
            _register.GetClass(s.ClassId).Name, StatisticsService.Format(_stats.StudentAverage(s.Id)) }));

    private void PrintSubjects() => ConsoleUi.Table(
        new[] { ("ID", 4), ("Учебен предмет", 37), ("Оценки", 7), ("Среден", 8) },
        Data.Subjects.OrderBy(s => s.Name).Select(s => new[] { s.Id.ToString(), s.Name,
            Data.Grades.Count(g => g.SubjectId == s.Id).ToString(),
            StatisticsService.Format(_stats.SubjectAverage(s.Id)) }));

    private void PrintClasses() => ConsoleUi.Table(
        new[] { ("ID", 4), ("Паралелка", 17), ("Ученици", 9), ("Среден успех", 14) },
        Data.Classes.OrderBy(c => c.Name).Select(c => new[] { c.Id.ToString(), c.Name,
            Data.Students.Count(s => s.ClassId == c.Id).ToString(),
            StatisticsService.Format(_stats.ClassAverage(c.Id)) }));

    private void PrintGrades(IEnumerable<Grade> grades) => ConsoleUi.Table(
        new[] { ("ID", 4), ("Дата", 10), ("Ученик", 24), ("Предмет", 22), ("Оц.", 4), ("Тип", 12) },
        grades.Select(g => new[] { g.Id.ToString(), g.Date.ToString("dd.MM.yyyy"),
            _register.GetStudent(g.StudentId).FullName, _register.GetSubject(g.SubjectId).Name,
            g.Value.ToString(), g.AssessmentType }));

    private void PrintAbsences(IEnumerable<Absence> absences) => ConsoleUi.Table(
        new[] { ("ID", 4), ("Дата", 10), ("Ученик", 24), ("Предмет", 22), ("Час.", 5), ("Статус", 10) },
        absences.Select(a => new[] { a.Id.ToString(), a.Date.ToString("dd.MM.yyyy"),
            _register.GetStudent(a.StudentId).FullName, _register.GetSubject(a.SubjectId).Name,
            a.Hours.ToString(), a.IsExcused ? "Извинено" : "Неизвинено" }));
}
