using EduTrack.Models;
using EduTrack.Services;

namespace EduTrack.UI;

public partial class ConsoleMenu
{
    private void GradesMenu()
    {
        while (true)
        {
            ConsoleUi.Header("ОЦЕНКИ", "Въвеждане, редактиране, филтриране и справки");
            ConsoleUi.Option(1, "Всички оценки");
            ConsoleUi.Option(2, "Добавяне на оценка");
            ConsoleUi.Option(3, "Редактиране на оценка");
            ConsoleUi.Option(4, "Изтриване на оценка");
            ConsoleUi.Option(5, "Филтриране и сортиране");
            ConsoleUi.Option(6, "Среден успех по предмет");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 6);
            switch (choice)
            {
                case 0: return;
                case 1:
                    ConsoleUi.Header("ВСИЧКИ ОЦЕНКИ");
                    PrintGrades(Data.Grades.OrderByDescending(g => g.Date).ThenByDescending(g => g.Id));
                    ConsoleUi.Pause(); break;
                case 2: AddGradeScreen(); break;
                case 3: EditGradeScreen(); break;
                case 4: DeleteGradeScreen(); break;
                case 5: FilterGradesScreen(); break;
                case 6:
                    ConsoleUi.Header("УСПЕХ ПО ПРЕДМЕТИ");
                    PrintSubjects(); ConsoleUi.Pause(); break;
            }
        }
    }

    private static string SelectAssessmentType()
    {
        ConsoleUi.Section("ВИД ОЦЕНЯВАНЕ");
        ConsoleUi.Option(1, "Текуща");
        ConsoleUi.Option(2, "Контролна работа");
        ConsoleUi.Option(3, "Класна работа");
        ConsoleUi.Option(4, "Устно изпитване");
        ConsoleUi.Option(5, "Проект");
        int choice = ConsoleUi.Number("Вид", 1, 5);
        return choice switch
        {
            1 => "Текуща", 2 => "Контролна", 3 => "Класна", 4 => "Устна", _ => "Проект"
        };
    }

    private void AddGradeScreen()
    {
        ConsoleUi.Header("ДОБАВЯНЕ НА ОЦЕНКА");
        var student = SelectStudent();
        if (student is null) return;
        var subject = SelectSubject();
        if (subject is null) return;
        int value = ConsoleUi.Number("Оценка", 2, 6);
        string type = SelectAssessmentType();
        Change(() => _register.AddGrade(student.Id, subject.Id, value, type), "Оценката е добавена.");
    }

    private void EditGradeScreen()
    {
        ConsoleUi.Header("РЕДАКТИРАНЕ НА ОЦЕНКА");
        var grade = SelectGrade();
        if (grade is null) return;
        int subjectId = grade.SubjectId;
        if (ConsoleUi.Confirm("Да променим ли предмета?"))
        {
            var subject = SelectSubject();
            if (subject is null) return;
            subjectId = subject.Id;
        }
        int value = ConsoleUi.NumberOrDefault("Нова оценка", 2, 6, grade.Value);
        string type = grade.AssessmentType;
        if (ConsoleUi.Confirm("Да променим ли вида оценяване?")) type = SelectAssessmentType();
        Change(() => _register.EditGrade(grade.Id, subjectId, value, type), "Оценката е редактирана.");
    }

    private void DeleteGradeScreen()
    {
        ConsoleUi.Header("ИЗТРИВАНЕ НА ОЦЕНКА");
        var grade = SelectGrade();
        if (grade is null || !ConsoleUi.Confirm($"Да се изтрие оценка {grade.Value} (ID {grade.Id})?")) return;
        Change(() => _register.DeleteGrade(grade.Id), "Оценката е премахната.");
    }

    private void FilterGradesScreen()
    {
        ConsoleUi.Header("ФИЛТЪР НА ОЦЕНКИТЕ");
        ConsoleUi.Option(1, "По ученик");
        ConsoleUi.Option(2, "По предмет");
        ConsoleUi.Option(3, "По паралелка");
        ConsoleUi.Option(4, "Всички оценки");
        ConsoleUi.Option(0, "Назад");
        int choice = ConsoleUi.Number("Филтър", 0, 4);
        if (choice == 0) return;
        IEnumerable<Grade> filtered = Data.Grades;
        if (choice == 1)
        {
            var student = SelectStudent();
            if (student is null) return;
            filtered = filtered.Where(g => g.StudentId == student.Id);
        }
        else if (choice == 2)
        {
            var subject = SelectSubject();
            if (subject is null) return;
            filtered = filtered.Where(g => g.SubjectId == subject.Id);
        }
        else if (choice == 3)
        {
            var schoolClass = SelectClass();
            if (schoolClass is null) return;
            var ids = Data.Students.Where(s => s.ClassId == schoolClass.Id).Select(s => s.Id).ToHashSet();
            filtered = filtered.Where(g => ids.Contains(g.StudentId));
        }
        ConsoleUi.Section("СОРТИРАНЕ");
        ConsoleUi.Option(1, "Най-нови първо");
        ConsoleUi.Option(2, "Най-високи оценки първо");
        ConsoleUi.Option(3, "Най-ниски оценки първо");
        ConsoleUi.Option(4, "По име на ученик");
        int sort = ConsoleUi.Number("Подредба", 1, 4);
        filtered = sort switch
        {
            2 => filtered.OrderByDescending(g => g.Value).ThenByDescending(g => g.Date),
            3 => filtered.OrderBy(g => g.Value).ThenByDescending(g => g.Date),
            4 => filtered.OrderBy(g => _register.GetStudent(g.StudentId).FullName),
            _ => filtered.OrderByDescending(g => g.Date).ThenByDescending(g => g.Id)
        };
        ConsoleUi.Section("РЕЗУЛТАТИ");
        PrintGrades(filtered);
        ConsoleUi.Pause();
    }

    private void AbsencesMenu()
    {
        while (true)
        {
            ConsoleUi.Header("ОТСЪСТВИЯ", "Статус, извиняване и статистика в учебни часове");
            ConsoleUi.Option(1, "Всички отсъствия");
            ConsoleUi.Option(2, "Добавяне на отсъствие");
            ConsoleUi.Option(3, "Извиняване на отсъствие");
            ConsoleUi.Option(4, "Отмяна на извинение");
            ConsoleUi.Option(5, "Изтриване на отсъствие");
            ConsoleUi.Option(6, "Статистика за ученик");
            ConsoleUi.Option(7, "Статистика за паралелка");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 7);
            switch (choice)
            {
                case 0: return;
                case 1:
                    ConsoleUi.Header("ВСИЧКИ ОТСЪСТВИЯ");
                    PrintAbsences(Data.Absences.OrderByDescending(a => a.Date).ThenByDescending(a => a.Id));
                    ConsoleUi.Pause(); break;
                case 2: AddAbsenceScreen(); break;
                case 3: ExcuseAbsenceScreen(true); break;
                case 4: ExcuseAbsenceScreen(false); break;
                case 5: DeleteAbsenceScreen(); break;
                case 6: StudentAbsenceStatsScreen(); break;
                case 7: ClassAbsenceStatsScreen(); break;
            }
        }
    }

    private void AddAbsenceScreen()
    {
        ConsoleUi.Header("ДОБАВЯНЕ НА ОТСЪСТВИЕ");
        var student = SelectStudent();
        if (student is null) return;
        var subject = SelectSubject();
        if (subject is null) return;
        DateTime date = ConsoleUi.Date("Дата");
        int hours = ConsoleUi.Number("Пропуснати учебни часове", 1, 8);
        Change(() => _register.AddAbsence(student.Id, subject.Id, date, hours), "Отсъствието е добавено.");
    }

    private void ExcuseAbsenceScreen(bool excuse)
    {
        ConsoleUi.Header(excuse ? "ИЗВИНЯВАНЕ" : "ОТМЯНА НА ИЗВИНЕНИЕ");
        var absence = SelectAbsence();
        if (absence is null) return;
        if (excuse && absence.IsExcused) { ConsoleUi.Info("Това отсъствие вече е извинено."); ConsoleUi.Pause(); return; }
        if (!excuse && !absence.IsExcused) { ConsoleUi.Info("Отсъствието и сега е неизвинено."); ConsoleUi.Pause(); return; }
        if (!ConsoleUi.Confirm(excuse ? "Да извините това отсъствие?" : "Да отмените извинението?")) return;
        string reason = excuse ? ConsoleUi.Text("Основание (напр. медицинска бележка)", 80) : "";
        Change(() => _register.SetAbsenceExcused(absence.Id, excuse, reason),
            excuse ? "Отсъствието е извинено." : "Извинението е отменено.");
    }

    private void DeleteAbsenceScreen()
    {
        ConsoleUi.Header("ИЗТРИВАНЕ НА ОТСЪСТВИЕ");
        var absence = SelectAbsence();
        if (absence is null || !ConsoleUi.Confirm($"Да се изтрие отсъствие ID {absence.Id}?")) return;
        Change(() => _register.DeleteAbsence(absence.Id), "Отсъствието е премахнато.");
    }

    private void StudentAbsenceStatsScreen()
    {
        ConsoleUi.Header("ОТСЪСТВИЯ ПО УЧЕНИК");
        var student = SelectStudent();
        if (student is null) return;
        ConsoleUi.Section(student.FullName);
        ConsoleUi.DashboardMetric("Извинени учебни часове", _stats.AbsenceHours(student.Id, excused: true).ToString());
        ConsoleUi.DashboardMetric("Неизвинени учебни часове", _stats.AbsenceHours(student.Id, excused: false).ToString());
        ConsoleUi.DashboardMetric("Общо учебни часове", _stats.AbsenceHours(student.Id).ToString());
        PrintAbsences(Data.Absences.Where(a => a.StudentId == student.Id).OrderByDescending(a => a.Date));
        ConsoleUi.Pause();
    }

    private void ClassAbsenceStatsScreen()
    {
        ConsoleUi.Header("ОТСЪСТВИЯ ПО ПАРАЛЕЛКА");
        var schoolClass = SelectClass();
        if (schoolClass is null) return;
        ConsoleUi.Section(schoolClass.Name);
        ConsoleUi.DashboardMetric("Извинени учебни часове", _stats.AbsenceHours(classId: schoolClass.Id, excused: true).ToString());
        ConsoleUi.DashboardMetric("Неизвинени учебни часове", _stats.AbsenceHours(classId: schoolClass.Id, excused: false).ToString());
        ConsoleUi.DashboardMetric("Общо учебни часове", _stats.AbsenceHours(classId: schoolClass.Id).ToString());
        var ids = Data.Students.Where(s => s.ClassId == schoolClass.Id).Select(s => s.Id).ToHashSet();
        PrintAbsences(Data.Absences.Where(a => ids.Contains(a.StudentId)).OrderByDescending(a => a.Date));
        ConsoleUi.Pause();
    }
}
