using EduTrack.Models;
using EduTrack.Services;

namespace EduTrack.UI;

public partial class ConsoleMenu
{
    private void SubjectsMenu()
    {
        while (true)
        {
            ConsoleUi.Header("УЧЕБНИ ПРЕДМЕТИ", "Каталог и анализ на резултатите");
            ConsoleUi.Option(1, "Списък и среден успех по предмет");
            ConsoleUi.Option(2, "Добавяне на предмет");
            ConsoleUi.Option(3, "Преименуване на предмет");
            ConsoleUi.Option(4, "Изтриване на предмет (само без свързани записи)");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 4);
            switch (choice)
            {
                case 0: return;
                case 1:
                    ConsoleUi.Header("КАТАЛОГ НА ПРЕДМЕТИТЕ");
                    PrintSubjects(); ConsoleUi.Pause(); break;
                case 2:
                    ConsoleUi.Header("НОВ ПРЕДМЕТ");
                    string name = ConsoleUi.Text("Наименование", 55);
                    Change(() => _register.AddSubject(name), "Предметът е добавен."); break;
                case 3:
                    ConsoleUi.Header("ПРЕИМЕНУВАНЕ НА ПРЕДМЕТ");
                    var subject = SelectSubject();
                    if (subject is null) break;
                    string newName = ConsoleUi.Text("Ново име", 55, subject.Name);
                    Change(() => _register.RenameSubject(subject.Id, newName), "Предметът е преименуван."); break;
                case 4:
                    ConsoleUi.Header("ИЗТРИВАНЕ НА ПРЕДМЕТ");
                    var toDelete = SelectSubject();
                    if (toDelete is null || !ConsoleUi.Confirm($"Да изтриете {toDelete.Name}?")) break;
                    Change(() => _register.DeleteSubject(toDelete.Id), "Предметът е изтрит."); break;
            }
        }
    }

    private void ClassesMenu()
    {
        while (true)
        {
            ConsoleUi.Header("УЧЕБНИ ПАРАЛЕЛКИ", "Управление на класове и разпределение");
            ConsoleUi.Option(1, "Списък и среден успех по паралелки");
            ConsoleUi.Option(2, "Добавяне на паралелка");
            ConsoleUi.Option(3, "Преименуване на паралелка");
            ConsoleUi.Option(4, "Изтриване на празна паралелка");
            ConsoleUi.Option(5, "Ученици от избрана паралелка");
            ConsoleUi.Option(6, "Преместване на ученик в друга паралелка");
            ConsoleUi.Option(7, "Сравнение между паралелките");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 7);
            switch (choice)
            {
                case 0: return;
                case 1:
                    ConsoleUi.Header("КАТАЛОГ НА ПАРАЛЕЛКИТЕ");
                    PrintClasses(); ConsoleUi.Pause(); break;
                case 2:
                    ConsoleUi.Header("НОВА ПАРАЛЕЛКА");
                    string name = ConsoleUi.Text("Име (пример: 10Г)", 8);
                    Change(() => _register.AddClass(name), "Паралелката е добавена."); break;
                case 3:
                    ConsoleUi.Header("ПРЕИМЕНУВАНЕ НА ПАРАЛЕЛКА");
                    var schoolClass = SelectClass();
                    if (schoolClass is null) break;
                    string newName = ConsoleUi.Text("Ново име", 8, schoolClass.Name);
                    Change(() => _register.RenameClass(schoolClass.Id, newName), "Паралелката е преименувана."); break;
                case 4:
                    ConsoleUi.Header("ИЗТРИВАНЕ НА ПАРАЛЕЛКА");
                    var classToDelete = SelectClass();
                    if (classToDelete is null || !ConsoleUi.Confirm($"Да изтриете {classToDelete.Name}?")) break;
                    Change(() => _register.DeleteClass(classToDelete.Id), "Празната паралелка е изтрита."); break;
                case 5:
                    ConsoleUi.Header("УЧЕНИЦИ ПО ПАРАЛЕЛКА");
                    var chosenClass = SelectClass();
                    if (chosenClass is null) break;
                    ConsoleUi.Section($"ПАРАЛЕЛКА {chosenClass.Name}");
                    PrintStudents(_register.StudentsSorted().Where(s => s.ClassId == chosenClass.Id));
                    ConsoleUi.Pause(); break;
                case 6: MoveStudentScreen(); break;
                case 7: ClassComparisonScreen(); break;
            }
        }
    }

    private void MoveStudentScreen()
    {
        ConsoleUi.Header("ПРЕМЕСТВАНЕ НА УЧЕНИК");
        var student = SelectStudent();
        if (student is null) return;
        var target = SelectClass();
        if (target is null) return;
        int number = ConsoleUi.Number("Нов номер в паралелката", 1, 40);
        Change(() => _register.EditStudent(student.Id, student.FirstName, student.LastName, target.Id, number),
            $"Ученикът е преместен в {target.Name}.");
    }

    private void ClassComparisonScreen()
    {
        ConsoleUi.Header("СРАВНЕНИЕ НА ПАРАЛЕЛКИТЕ");
        ConsoleUi.Table(new[] { ("Паралелка", 12), ("Ученици", 9), ("Оценки", 8), ("Среден успех", 14), ("Неизвинени ч.", 14) },
            _stats.ClassRanking().Select(x => new[] {
                x.Class.Name,
                x.Students.ToString(),
                Data.Grades.Count(g => Data.Students.Any(s => s.Id == g.StudentId && s.ClassId == x.Class.Id)).ToString(),
                StatisticsService.Format(x.Average),
                _stats.AbsenceHours(classId: x.Class.Id, excused: false).ToString()
            }));
        ConsoleUi.Pause();
    }

    private void StatisticsMenu()
    {
        while (true)
        {
            ConsoleUi.Header("СТАТИСТИКА", "Точни изчисления на база реалните записи");
            ConsoleUi.Option(1, "Обща училищна статистика");
            ConsoleUi.Option(2, "TOP 5 ученици");
            ConsoleUi.Option(3, "Разпределение на оценките");
            ConsoleUi.Option(4, "Най-силен / най-слаб предмет");
            ConsoleUi.Option(5, "Среден успех по предмети");
            ConsoleUi.Option(6, "Сравнение между паралелките");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 6);
            switch (choice)
            {
                case 0: return;
                case 1: GeneralStatisticsScreen(); break;
                case 2: TopStudentsScreen(); break;
                case 3: GradeDistributionScreen(); break;
                case 4: BestWorstSubjectsScreen(); break;
                case 5: ConsoleUi.Header("УСПЕХ ПО ПРЕДМЕТИ"); PrintSubjects(); ConsoleUi.Pause(); break;
                case 6: ClassComparisonScreen(); break;
            }
        }
    }

    private void GeneralStatisticsScreen()
    {
        ConsoleUi.Header("ОБЩА СТАТИСТИКА");
        var top = _stats.TopStudents(1).FirstOrDefault();
        ConsoleUi.DashboardMetric("Брой ученици", Data.Students.Count.ToString());
        ConsoleUi.DashboardMetric("Брой паралелки", Data.Classes.Count.ToString());
        ConsoleUi.DashboardMetric("Брой предмети", Data.Subjects.Count.ToString());
        ConsoleUi.DashboardMetric("Общо оценки", Data.Grades.Count.ToString());
        ConsoleUi.DashboardMetric("Среден успех", StatisticsService.Format(_stats.OverallAverage()));
        ConsoleUi.DashboardMetric("Най-висок среден успех", top.Student is null ? "няма данни" :
            $"{top.Student.FullName} ({StatisticsService.Format(top.Average)})");
        ConsoleUi.DashboardMetric("Извинени часове", _stats.AbsenceHours(excused: true).ToString());
        ConsoleUi.DashboardMetric("Неизвинени часове", _stats.AbsenceHours(excused: false).ToString());
        ConsoleUi.DashboardMetric("Всички отсъствия (часове)", _stats.AbsenceHours().ToString());
        ConsoleUi.DashboardMetric("Автоматични известия", _alerts.GetAlerts().Count.ToString());
        ConsoleUi.Info("Средният успех се изчислява от всички оценки; празните групи са 'няма'.");
        ConsoleUi.Pause();
    }

    private void TopStudentsScreen()
    {
        ConsoleUi.Header("КЛАСАЦИЯ - ТОП 5 УЧЕНИЦИ");
        int position = 0;
        ConsoleUi.Table(new[] { ("Място", 7), ("Ученик", 31), ("Паралелка", 12), ("Среден", 9), ("Оценки", 7) },
            _stats.TopStudents().Select(x => new[] { (++position).ToString(), x.Student.FullName,
                _register.GetClass(x.Student.ClassId).Name,
                StatisticsService.Format(x.Average), x.GradeCount.ToString() }));
        ConsoleUi.Pause();
    }

    private void GradeDistributionScreen()
    {
        ConsoleUi.Header("РАЗПРЕДЕЛЕНИЕ НА ОЦЕНКИТЕ");
        var distribution = _stats.GradeDistribution();
        int total = distribution.Values.Sum();
        foreach (var pair in distribution.OrderByDescending(p => p.Key))
        {
            double percent = total == 0 ? 0 : 100.0 * pair.Value / total;
            ConsoleUi.ColorWrite($"  Оценка {pair.Key}  ", ConsoleColor.White);
            ConsoleUi.ColorWrite(new string('█', Math.Min(pair.Value, 40)), ConsoleColor.Cyan);
            Console.WriteLine($"  {pair.Value} бр. / {percent:F1}%");
        }
        ConsoleUi.Info($"Общо: {total} оценки. Един блок = една оценка (до 40 блока на ред).");
        ConsoleUi.Pause();
    }

    private void BestWorstSubjectsScreen()
    {
        ConsoleUi.Header("ПРЕДМЕТИ - СИЛНИ И СЛАБИ СТРАНИ");
        var ranking = _stats.SubjectRanking().ToList();
        if (ranking.Count == 0) ConsoleUi.Info("Все още няма оценки по учебни предмети.");
        else
        {
            var best = ranking.First();
            var worst = ranking.Last();
            ConsoleUi.DashboardMetric("Най-силен предмет", $"{best.Subject.Name}: {StatisticsService.Format(best.Average)}");
            ConsoleUi.DashboardMetric("Най-слаб предмет", $"{worst.Subject.Name}: {StatisticsService.Format(worst.Average)}");
            ConsoleUi.Info("В сравненията участват само предмети с поне една оценка.");
        }
        ConsoleUi.Pause();
    }
}
