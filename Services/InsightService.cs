using EduTrack.Models;

namespace EduTrack.Services;

public enum Trend { NotEnoughData, Improving, Declining, Stable }

public class InsightReport
{
    public double? Average { get; set; }
    public string StrongestSubject { get; set; } = "Няма данни";
    public string WeakestSubject { get; set; } = "Няма данни";
    public Trend Trend { get; set; }
    public double TrendDifference { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

// Последни 2 оценки срещу предходните 2; +/-0.50 е отчетлива тенденция.
public class InsightService
{
    private readonly SchoolRegister _register;
    private readonly StatisticsService _stats;
    public InsightService(SchoolRegister register, StatisticsService stats)
    {
        _register = register; _stats = stats;
    }

    public (Trend Direction, double Difference) AnalyzeTrend(int studentId)
    {
        var grades = _register.Data.Grades.Where(g => g.StudentId == studentId)
            .OrderBy(g => g.Date).ThenBy(g => g.Id).ToList();
        if (grades.Count < 4) return (Trend.NotEnoughData, 0);
        double previous = grades.Skip(grades.Count - 4).Take(2).Average(g => g.Value);
        double recent = grades.TakeLast(2).Average(g => g.Value);
        double difference = recent - previous;
        if (difference >= 0.5) return (Trend.Improving, difference);
        if (difference <= -0.5) return (Trend.Declining, difference);
        return (Trend.Stable, difference);
    }

    public InsightReport GetReport(int studentId)
    {
        _register.GetStudent(studentId);
        var report = new InsightReport { Average = _stats.StudentAverage(studentId) };
        var subjectStats = _register.Data.Grades.Where(g => g.StudentId == studentId)
            .GroupBy(g => g.SubjectId)
            .Select(group => new { Subject = _register.GetSubject(group.Key), Avg = group.Average(g => g.Value) })
            .OrderByDescending(x => x.Avg).ToList();
        if (subjectStats.Count > 0)
        {
            report.StrongestSubject = $"{subjectStats.First().Subject.Name} ({subjectStats.First().Avg:F2})";
            report.WeakestSubject = $"{subjectStats.Last().Subject.Name} ({subjectStats.Last().Avg:F2})";
        }
        var trend = AnalyzeTrend(studentId);
        report.Trend = trend.Direction;
        report.TrendDifference = trend.Difference;

        var lowSubjects = subjectStats.Where(s => s.Avg < 4.0).Select(s => s.Subject.Name).ToList();
        if (lowSubjects.Count > 0)
            report.Recommendations.Add($"Упражнения и консултация по: {string.Join(", ", lowSubjects)}.");
        if (trend.Direction == Trend.Declining)
            report.Recommendations.Add("Последните оценки са по-ниски. Преговорете последните уроци.");
        if (trend.Direction == Trend.Improving)
            report.Recommendations.Add("Има положителна тенденция. Продължете с редовната подготовка.");
        if (subjectStats.Count == 0)
            report.Recommendations.Add("Добавете оценки, за да се появи персонален анализ.");
        else if (report.Recommendations.Count == 0)
            report.Recommendations.Add("Резултатите са стабилни. Поддържайте постоянен график за учене.");
        return report;
    }

    public (double? OldSubject, double NewSubject, double? OldOverall, double NewOverall) Simulate(
        int studentId, int subjectId, int futureGrade)
    {
        _register.GetStudent(studentId); _register.GetSubject(subjectId);
        if (futureGrade is < 2 or > 6) throw new ArgumentException("Бъдещата оценка е между 2 и 6.");
        var subjectGrades = _register.Data.Grades.Where(g => g.StudentId == studentId && g.SubjectId == subjectId).ToList();
        var allGrades = _register.Data.Grades.Where(g => g.StudentId == studentId).ToList();
        return (
            _stats.StudentSubjectAverage(studentId, subjectId),
            (subjectGrades.Sum(g => g.Value) + futureGrade) / (double)(subjectGrades.Count + 1),
            _stats.StudentAverage(studentId),
            (allGrades.Sum(g => g.Value) + futureGrade) / (double)(allGrades.Count + 1)
        );
        // Няма промяна в Data: симулацията е само изчисление.
    }

    public static string TrendLabel(Trend trend) => trend switch
    {
        Trend.Improving => "Подобрение",
        Trend.Declining => "Спад",
        Trend.Stable => "Без значителна промяна",
        _ => "Нужни са поне 4 оценки"
    };
}
