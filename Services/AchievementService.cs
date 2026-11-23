using EduTrack.Models;

namespace EduTrack.Services;

// Постиженията се преизчисляват от текущите данни - не се пазят фиктивни значки.
public class AchievementService
{
    private readonly SchoolRegister _register;
    private readonly StatisticsService _stats;
    private readonly InsightService _insights;

    public AchievementService(SchoolRegister register, StatisticsService stats, InsightService insights)
    {
        _register = register; _stats = stats; _insights = insights;
    }

    public List<string> ForStudent(int studentId)
    {
        _register.GetStudent(studentId);
        var grades = _register.Data.Grades.Where(g => g.StudentId == studentId).ToList();
        var absences = _register.Data.Absences.Where(a => a.StudentId == studentId).ToList();
        var result = new List<string>();
        double? average = _stats.StudentAverage(studentId);
        if (grades.Count >= 5 && average >= 5.50)
            result.Add("Отличен успех - поне 5 оценки и среден успех 5,50+");
        if (grades.Count >= 4 && _insights.AnalyzeTrend(studentId).Direction == Trend.Improving)
            result.Add("Напредък - последните 2 оценки са поне с 0,50 по-добри");
        if (absences.All(a => a.IsExcused))
            result.Add("Отговорност - няма неизвинени отсъствия");
        if (grades.Count >= 8 && grades.Select(g => g.SubjectId).Distinct().Count() >= 3)
            result.Add("Постоянство - поне 8 оценки по 3 различни предмета");
        return result;
    }
}
