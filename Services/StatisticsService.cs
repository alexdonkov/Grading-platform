using EduTrack.Models;

namespace EduTrack.Services;

// Правило: средният успех е средноаритметично на всички включени оценки.
// Ученици / предмети / паралелки без оценки нямат изчислен среден успех.
public class StatisticsService
{
    private readonly SchoolRegister _register;
    private SchoolData Data => _register.Data;
    public StatisticsService(SchoolRegister register) => _register = register;

    public double? OverallAverage() => Average(Data.Grades);
    public double? StudentAverage(int studentId) => Average(Data.Grades.Where(g => g.StudentId == studentId));
    public double? SubjectAverage(int subjectId) => Average(Data.Grades.Where(g => g.SubjectId == subjectId));
    public double? ClassAverage(int classId)
    {
        var studentIds = Data.Students.Where(s => s.ClassId == classId).Select(s => s.Id).ToHashSet();
        return Average(Data.Grades.Where(g => studentIds.Contains(g.StudentId)));
    }
    public double? StudentSubjectAverage(int studentId, int subjectId) =>
        Average(Data.Grades.Where(g => g.StudentId == studentId && g.SubjectId == subjectId));

    public static double? Average(IEnumerable<Grade> grades)
    {
        var values = grades.Select(g => g.Value).ToList();
        return values.Count == 0 ? null : values.Average();
    }

    public IEnumerable<(Student Student, double Average, int GradeCount)> TopStudents(int limit = 5) =>
        Data.Students
            .Select(s => (Student: s, Average: StudentAverage(s.Id), GradeCount: Data.Grades.Count(g => g.StudentId == s.Id)))
            .Where(x => x.Average.HasValue)
            .OrderByDescending(x => x.Average!.Value)
            .ThenByDescending(x => x.GradeCount)
            .ThenBy(x => x.Student.FullName)
            .Take(limit)
            .Select(x => (x.Student, x.Average!.Value, x.GradeCount));

    public IEnumerable<(Subject Subject, double Average, int GradeCount)> SubjectRanking() =>
        Data.Subjects
            .Select(s => (Subject: s, Average: SubjectAverage(s.Id), GradeCount: Data.Grades.Count(g => g.SubjectId == s.Id)))
            .Where(x => x.Average.HasValue)
            .OrderByDescending(x => x.Average!.Value)
            .ThenBy(x => x.Subject.Name)
            .Select(x => (x.Subject, x.Average!.Value, x.GradeCount));

    public IEnumerable<(SchoolClass Class, double? Average, int Students)> ClassRanking() =>
        Data.Classes
            .Select(c => (Class: c, Average: ClassAverage(c.Id), Students: Data.Students.Count(s => s.ClassId == c.Id)))
            .OrderByDescending(x => x.Average.HasValue)
            .ThenByDescending(x => x.Average)
            .ThenBy(x => x.Class.Name);

    public Dictionary<int, int> GradeDistribution() =>
        Enumerable.Range(2, 5).ToDictionary(value => value, value => Data.Grades.Count(g => g.Value == value));

    public int AbsenceHours(int? studentId = null, int? classId = null, bool? excused = null)
    {
        IEnumerable<Absence> query = Data.Absences;
        if (studentId.HasValue) query = query.Where(a => a.StudentId == studentId.Value);
        if (classId.HasValue)
        {
            var ids = Data.Students.Where(s => s.ClassId == classId.Value).Select(s => s.Id).ToHashSet();
            query = query.Where(a => ids.Contains(a.StudentId));
        }
        if (excused.HasValue) query = query.Where(a => a.IsExcused == excused.Value);
        return query.Sum(a => a.Hours);
    }

    public static string Format(double? average) => average.HasValue
        ? average.Value.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("bg-BG")) : "няма";
}
