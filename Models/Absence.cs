namespace EduTrack.Models;

public class Absence
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public DateTime Date { get; set; }
    public int Hours { get; set; }
    public bool IsExcused { get; set; }
    public string ExcuseReason { get; set; } = "";

    public Absence() { }

    public Absence(int id, int studentId, int subjectId, DateTime date, int hours)
    {
        Id = id;
        StudentId = studentId;
        SubjectId = subjectId;
        Date = date.Date;
        Hours = hours;
    }
}
