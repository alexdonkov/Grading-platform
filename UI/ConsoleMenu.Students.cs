using EduTrack.Models;
using EduTrack.Services;

namespace EduTrack.UI;

public partial class ConsoleMenu
{
    private void StudentsMenu()
    {
        while (true)
        {
            ConsoleUi.Header("УЧЕНИЦИ", "Списъци, профили и история");
            ConsoleUi.Option(1, "Всички ученици");
            ConsoleUi.Option(2, "Добавяне на ученик");
            ConsoleUi.Option(3, "Търсене по име, клас или номер");
            ConsoleUi.Option(4, "Индивидуален профил");
            ConsoleUi.Option(5, "Редактиране на ученик");
            ConsoleUi.Option(6, "Изтриване на ученик");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 6);
            switch (choice)
            {
                case 0: return;
                case 1:
                    ConsoleUi.Header("РЕГИСТЪР НА УЧЕНИЦИТЕ");
                    PrintStudents(_register.StudentsSorted()); ConsoleUi.Pause(); break;
                case 2: AddStudentScreen(); break;
                case 3: SearchStudentsScreen(); break;
                case 4: StudentProfileScreen(); break;
                case 5: EditStudentScreen(); break;
                case 6: DeleteStudentScreen(); break;
            }
        }
    }

    private void AddStudentScreen()
    {
        ConsoleUi.Header("НОВ УЧЕНИК");
        var schoolClass = SelectClass();
        if (schoolClass is null) return;
        string first = ConsoleUi.Text("Име", 40);
        string last = ConsoleUi.Text("Фамилия", 40);
        int number = ConsoleUi.Number("Номер в паралелката", 1, 40);
        Change(() => _register.AddStudent(first, last, schoolClass.Id, number), "Ученикът е добавен.");
    }

    private void SearchStudentsScreen()
    {
        ConsoleUi.Header("ТЪРСЕНЕ НА УЧЕНИЦИ");
        ConsoleUi.Option(1, "По име или фамилия");
        ConsoleUi.Option(2, "По паралелка");
        ConsoleUi.Option(3, "По номер в клас");
        ConsoleUi.Option(0, "Назад");
        int option = ConsoleUi.Number("Избор", 0, 3);
        if (option == 0) return;
        IEnumerable<Student> result;
        if (option == 1)
        {
            string keyword = ConsoleUi.Text("Търсене", 40);
            result = _register.StudentsSorted().Where(s => s.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        else if (option == 2)
        {
            var schoolClass = SelectClass();
            if (schoolClass is null) return;
            result = _register.StudentsSorted().Where(s => s.ClassId == schoolClass.Id);
        }
        else
        {
            int number = ConsoleUi.Number("Номер", 1, 40);
            result = _register.StudentsSorted().Where(s => s.Number == number);
        }
        ConsoleUi.Section("РЕЗУЛТАТИ");
        PrintStudents(result); ConsoleUi.Pause();
    }

    private void StudentProfileScreen()
    {
        ConsoleUi.Header("ПРОФИЛ НА УЧЕНИК");
        var student = SelectStudent();
        if (student is null) return;
        ConsoleUi.Header($"ПРОФИЛ: {student.FullName.ToUpperInvariant()}");
        ConsoleUi.DashboardMetric("ID", student.Id.ToString());
        ConsoleUi.DashboardMetric("Паралелка", _register.GetClass(student.ClassId).Name);
        ConsoleUi.DashboardMetric("Номер", student.Number.ToString());
        ConsoleUi.DashboardMetric("Среден успех", StatisticsService.Format(_stats.StudentAverage(student.Id)));
        ConsoleUi.DashboardMetric("Извинени часове", _stats.AbsenceHours(student.Id, excused: true).ToString());
        ConsoleUi.DashboardMetric("Неизвинени часове", _stats.AbsenceHours(student.Id, excused: false).ToString());
        ConsoleUi.Section("ИСТОРИЯ НА ОЦЕНКИТЕ");
        PrintGrades(Data.Grades.Where(g => g.StudentId == student.Id).OrderByDescending(g => g.Date).ThenByDescending(g => g.Id));
        ConsoleUi.Section("ИСТОРИЯ НА ОТСЪСТВИЯТА");
        PrintAbsences(Data.Absences.Where(a => a.StudentId == student.Id).OrderByDescending(a => a.Date));
        ConsoleUi.Section("ПОСТИЖЕНИЯ");
        var awards = _achievements.ForStudent(student.Id);
        if (awards.Count == 0) ConsoleUi.Info("Още няма отключени постижения.");
        else foreach (string award in awards) ConsoleUi.Success(award);
        ConsoleUi.Pause();
    }

    private void EditStudentScreen()
    {
        ConsoleUi.Header("РЕДАКТИРАНЕ НА УЧЕНИК");
        var student = SelectStudent();
        if (student is null) return;
        string first = ConsoleUi.Text("Име", 40, student.FirstName);
        string last = ConsoleUi.Text("Фамилия", 40, student.LastName);
        int classId = student.ClassId;
        if (ConsoleUi.Confirm("Да променим ли паралелката?"))
        {
            var schoolClass = SelectClass();
            if (schoolClass is null) return;
            classId = schoolClass.Id;
        }
        int number = ConsoleUi.NumberOrDefault("Номер", 1, 40, student.Number);
        Change(() => _register.EditStudent(student.Id, first, last, classId, number), "Данните на ученика са обновени.");
    }

    private void DeleteStudentScreen()
    {
        ConsoleUi.Header("ИЗТРИВАНЕ НА УЧЕНИК");
        var student = SelectStudent();
        if (student is null) return;
        int gradeCount = Data.Grades.Count(g => g.StudentId == student.Id);
        int absenceCount = Data.Absences.Count(a => a.StudentId == student.Id);
        ConsoleUi.Warning($"Ще се изтрият и {gradeCount} оценки и {absenceCount} записа за отсъствия.");
        if (!ConsoleUi.Confirm($"Потвърждавате ли изтриването на {student.FullName}?")) return;
        Change(() => _register.DeleteStudent(student.Id), "Ученикът и свързаните му записи са премахнати.");
    }
}
