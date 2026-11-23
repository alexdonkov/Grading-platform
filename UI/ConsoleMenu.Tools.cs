using EduTrack.Services;

namespace EduTrack.UI;

public partial class ConsoleMenu
{
    private void ToolsMenu()
    {
        while (true)
        {
            ConsoleUi.Header("ИНТЕЛИГЕНТНИ ИНСТРУМЕНТИ", "Анализ, прогноза и автоматични постижения");
            ConsoleUi.Option(1, "EduInsights - анализ на ученик");
            ConsoleUi.Option(2, "Grade Simulator - симулатор на успех");
            ConsoleUi.Option(3, "Achievement System - постижения");
            ConsoleUi.Option(0, "Назад");
            int choice = ConsoleUi.Number("Избор", 0, 3);
            switch (choice)
            {
                case 0: return;
                case 1: InsightScreen(); break;
                case 2: SimulatorScreen(); break;
                case 3: AchievementScreen(); break;
            }
        }
    }

    private void InsightScreen()
    {
        ConsoleUi.Header("EDUINSIGHTS - ПЕРСОНАЛЕН АНАЛИЗ");
        var student = SelectStudent();
        if (student is null) return;
        var report = _insights.GetReport(student.Id);
        ConsoleUi.Header($"АНАЛИЗ: {student.FullName.ToUpperInvariant()}");
        ConsoleUi.DashboardMetric("Общ среден успех", StatisticsService.Format(report.Average));
        ConsoleUi.DashboardMetric("Най-силен предмет", report.StrongestSubject);
        ConsoleUi.DashboardMetric("Най-слаб предмет", report.WeakestSubject);
        ConsoleUi.DashboardMetric("Тенденция", InsightService.TrendLabel(report.Trend));
        if (report.Trend != Trend.NotEnoughData)
            ConsoleUi.DashboardMetric("Разлика (последни 2 / предишни 2)",
                report.TrendDifference.ToString("+0.00;-0.00;0.00"));
        ConsoleUi.Section("ПРЕПОРЪКИ ОТ СИСТЕМАТА");
        foreach (string recommendation in report.Recommendations) ConsoleUi.Info(recommendation);
        ConsoleUi.Info("Тези препоръки са по математически правила, а не от външен AI.");
        ConsoleUi.Pause();
    }

    private void SimulatorScreen()
    {
        ConsoleUi.Header("СИМУЛАТОР НА УСПЕХА", "Какво би станало при следваща оценка?");
        var student = SelectStudent();
        if (student is null) return;
        var subject = SelectSubject();
        if (subject is null) return;
        int futureGrade = ConsoleUi.Number("Желана бъдеща оценка", 2, 6);
        var prediction = _insights.Simulate(student.Id, subject.Id, futureGrade);
        ConsoleUi.Section($"ПРОГНОЗА ЗА {student.FullName.ToUpperInvariant()}");
        ConsoleUi.DashboardMetric("Учебен предмет", subject.Name);
        ConsoleUi.DashboardMetric("Хипотетична оценка", futureGrade.ToString());
        ConsoleUi.DashboardMetric("Сега по предмета", StatisticsService.Format(prediction.OldSubject));
        ConsoleUi.DashboardMetric("След оценката по предмета", StatisticsService.Format(prediction.NewSubject));
        ConsoleUi.DashboardMetric("Сега - общ успех", StatisticsService.Format(prediction.OldOverall));
        ConsoleUi.DashboardMetric("След оценката - общ успех", StatisticsService.Format(prediction.NewOverall));
        ConsoleUi.Info("Това е симулация. НИТО ЕДНА оценка не е записана или променена.");
        ConsoleUi.Pause();
    }

    private void AchievementScreen()
    {
        ConsoleUi.Header("УЧЕНИЧЕСКИ ПОСТИЖЕНИЯ", "Постижения, получени по ясни и обективни правила");
        var student = SelectStudent();
        if (student is null) return;
        ConsoleUi.Section(student.FullName);
        var achievements = _achievements.ForStudent(student.Id);
        if (achievements.Count == 0) ConsoleUi.Info("Няма отключени постижения към момента.");
        else foreach (string achievement in achievements) ConsoleUi.Success(achievement);
        ConsoleUi.Info("Постиженията се преизчисляват при всяка справка.");
        ConsoleUi.Pause();
    }

    private void AlertsScreen()
    {
        ConsoleUi.Header("АВТОМАТИЧНИ ИЗВЕСТИЯ", "Автоматични предупреждения от училищния дневник");
        ConsoleUi.DashboardMetric("Нисък успех", "< 3,50 при поне 3 оценки");
        ConsoleUi.DashboardMetric("Неизвинени отсъствия", "3 или повече учебни часа");
        ConsoleUi.DashboardMetric("Значителен спад", "поне -1,00 за последните 2 спрямо предходните 2");
        ConsoleUi.Section("АКТИВНИ СЪОБЩЕНИЯ");
        var alerts = _alerts.GetAlerts();
        if (alerts.Count == 0) ConsoleUi.Success("Няма активни предупреждения.");
        else
        {
            foreach (var alert in alerts) ConsoleUi.Warning($"[ID {alert.StudentId}] {alert.StudentName}: {alert.Message}");
            ConsoleUi.Info($"Общо {alerts.Count} предупреждения (един ученик може да има повече от едно).");
        }
        ConsoleUi.Pause();
    }
}
