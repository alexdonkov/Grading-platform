using EduTrack.Models;

namespace EduTrack.Services;

public class SmartAlert
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string Message { get; set; } = "";
}

public class AlertService
{
    private readonly SchoolRegister _register;
    private readonly StatisticsService _stats;
    private readonly InsightService _insights;

    public AlertService(SchoolRegister register, StatisticsService stats, InsightService insights)
    {
        _register = register; _stats = stats; _insights = insights;
    }

    public List<SmartAlert> GetAlerts()
    {
        var alerts = new List<SmartAlert>();
        foreach (var student in _register.StudentsSorted())
        {
            int count = _register.Data.Grades.Count(g => g.StudentId == student.Id);
            double? average = _stats.StudentAverage(student.Id);
            if (count >= 3 && average < 3.50)
                alerts.Add(new SmartAlert { StudentId = student.Id, StudentName = student.FullName,
                    Message = $"Среден успех под 3,50 ({StatisticsService.Format(average)})." });
            int unexcused = _stats.AbsenceHours(studentId: student.Id, excused: false);
            if (unexcused >= 3)
                alerts.Add(new SmartAlert { StudentId = student.Id, StudentName = student.FullName,
                    Message = $"Натрупани {unexcused} неизвинени учебни часа (праг: 3)." });
            var trend = _insights.AnalyzeTrend(student.Id);
            if (trend.Direction == Trend.Declining && trend.Difference <= -1.0)
                alerts.Add(new SmartAlert { StudentId = student.Id, StudentName = student.FullName,
                    Message = $"Значителен спад в последните оценки ({trend.Difference:+0.00;-0.00;0.00})." });
        }
        return alerts;
    }
}
