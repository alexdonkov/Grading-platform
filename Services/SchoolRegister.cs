using System.Text.RegularExpressions;
using EduTrack.Models;

namespace EduTrack.Services;

// Роля 1: бизнес правила; никакъв Console.ReadLine / Console.WriteLine тук.
public class SchoolRegister
{
    public SchoolData Data { get; }
    public SchoolRegister(SchoolData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Data.EnsureCounters();
    }

    public Student GetStudent(int id) => Data.Students.FirstOrDefault(s => s.Id == id)
        ?? throw new ArgumentException("Не съществува ученик с този ID.");
    public SchoolClass GetClass(int id) => Data.Classes.FirstOrDefault(c => c.Id == id)
        ?? throw new ArgumentException("Не съществува такава паралелка.");
    public Subject GetSubject(int id) => Data.Subjects.FirstOrDefault(s => s.Id == id)
        ?? throw new ArgumentException("Не съществува такъв предмет.");
    public Grade GetGrade(int id) => Data.Grades.FirstOrDefault(g => g.Id == id)
        ?? throw new ArgumentException("Не съществува оценка с този ID.");
    public Absence GetAbsence(int id) => Data.Absences.FirstOrDefault(a => a.Id == id)
        ?? throw new ArgumentException("Не съществува отсъствие с този ID.");

    public IEnumerable<Student> StudentsSorted() => Data.Students
        .OrderBy(s => GetClass(s.ClassId).Name).ThenBy(s => s.Number);

    public Student AddStudent(string firstName, string lastName, int classId, int number)
    {
        ValidateStudent(firstName, lastName, classId, number);
        var student = new Student(Data.NextStudentId++, firstName.Trim(), lastName.Trim(), classId, number);
        Data.Students.Add(student);
        return student;
    }

    public void EditStudent(int id, string firstName, string lastName, int classId, int number)
    {
        var student = GetStudent(id);
        ValidateStudent(firstName, lastName, classId, number, id);
        student.FirstName = firstName.Trim();
        student.LastName = lastName.Trim();
        student.ClassId = classId;
        student.Number = number;
    }

    public void DeleteStudent(int id)
    {
        var student = GetStudent(id);
        Data.Grades.RemoveAll(g => g.StudentId == id);
        Data.Absences.RemoveAll(a => a.StudentId == id);
        Data.Students.Remove(student);
    }

    private void ValidateStudent(string first, string last, int classId, int number, int editingId = 0)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last)
            || first.Trim().Length > 40 || last.Trim().Length > 40)
            throw new ArgumentException("Името и фамилията трябва да са между 1 и 40 символа.");
        GetClass(classId);
        if (number < 1 || number > 40)
            throw new ArgumentException("Номерът трябва да е от 1 до 40.");
        if (Data.Students.Any(s => s.Id != editingId && s.ClassId == classId && s.Number == number))
            throw new InvalidOperationException("В тази паралелка вече има ученик с този номер.");
    }

    public SchoolClass AddClass(string name)
    {
        string normalized = NormalizeClassName(name);
        if (Data.Classes.Any(c => c.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Тази паралелка вече съществува.");
        var schoolClass = new SchoolClass(Data.NextClassId++, normalized);
        Data.Classes.Add(schoolClass);
        return schoolClass;
    }

    public void RenameClass(int id, string name)
    {
        var schoolClass = GetClass(id);
        string normalized = NormalizeClassName(name);
        if (Data.Classes.Any(c => c.Id != id && c.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Друга паралелка вече носи това име.");
        schoolClass.Name = normalized;
    }

    public void DeleteClass(int id)
    {
        var schoolClass = GetClass(id);
        if (Data.Students.Any(s => s.ClassId == id))
            throw new InvalidOperationException("Първо преместете или изтрийте учениците от тази паралелка.");
        Data.Classes.Remove(schoolClass);
    }

    private static string NormalizeClassName(string name)
    {
        string result = (name ?? "").Trim().Replace(" ", "").ToUpperInvariant();
        if (!Regex.IsMatch(result, "^(1[0-2]|[1-9])[А-ЯA-Z]{1,2}$"))
            throw new ArgumentException("Въведете паралелка във формат 10А, 10Б или 9В (1.-12. клас).");
        return result;
    }

    public Subject AddSubject(string name)
    {
        ValidateSubjectName(name);
        var subject = new Subject(Data.NextSubjectId++, name.Trim());
        Data.Subjects.Add(subject);
        return subject;
    }

    public void RenameSubject(int id, string name)
    {
        var subject = GetSubject(id);
        ValidateSubjectName(name, id);
        subject.Name = name.Trim();
    }

    public void DeleteSubject(int id)
    {
        var subject = GetSubject(id);
        if (Data.Grades.Any(g => g.SubjectId == id) || Data.Absences.Any(a => a.SubjectId == id))
            throw new InvalidOperationException("Предметът има оценки или отсъствия и не може да бъде изтрит.");
        Data.Subjects.Remove(subject);
    }

    private void ValidateSubjectName(string? name, int editingId = 0)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 55)
            throw new ArgumentException("Името на предмета трябва да е между 1 и 55 символа.");
        if (Data.Subjects.Any(s => s.Id != editingId && s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Такъв предмет вече съществува.");
    }

    public Grade AddGrade(int studentId, int subjectId, int value, string assessmentType)
    {
        ValidateGrade(studentId, subjectId, value, assessmentType);
        var grade = new Grade(Data.NextGradeId++, studentId, subjectId, value, assessmentType.Trim());
        Data.Grades.Add(grade);
        return grade;
    }

    public void EditGrade(int id, int subjectId, int value, string assessmentType)
    {
        var grade = GetGrade(id);
        ValidateGrade(grade.StudentId, subjectId, value, assessmentType);
        grade.SubjectId = subjectId;
        grade.Value = value;
        grade.AssessmentType = assessmentType.Trim();
        // Датата на първоначалното въвеждане се запазва.
    }

    public void DeleteGrade(int id) => Data.Grades.Remove(GetGrade(id));

    private void ValidateGrade(int studentId, int subjectId, int value, string type)
    {
        GetStudent(studentId);
        GetSubject(subjectId);
        if (value < 2 || value > 6)
            throw new ArgumentException("Оценката трябва да е цяло число от 2 до 6.");
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 35)
            throw new ArgumentException("Видът на оценяването трябва да е между 1 и 35 символа.");
    }

    public Absence AddAbsence(int studentId, int subjectId, DateTime date, int hours)
    {
        GetStudent(studentId); GetSubject(subjectId);
        if (hours < 1 || hours > 8)
            throw new ArgumentException("Броят часове трябва да е между 1 и 8.");
        if (date.Date > DateTime.Today || date.Date < DateTime.Today.AddYears(-5))
            throw new ArgumentException("Датата трябва да е от последните 5 години и не по-късна от днес.");
        var absence = new Absence(Data.NextAbsenceId++, studentId, subjectId, date, hours);
        Data.Absences.Add(absence);
        return absence;
    }

    public void SetAbsenceExcused(int id, bool isExcused, string reason = "")
    {
        var absence = GetAbsence(id);
        if (isExcused && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 80))
            throw new ArgumentException("Посочете причина за извинението (до 80 символа).");
        absence.IsExcused = isExcused;
        absence.ExcuseReason = isExcused ? reason.Trim() : "";
    }

    public void DeleteAbsence(int id) => Data.Absences.Remove(GetAbsence(id));
}
