namespace EduTrack.Models;

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int Value { get; set; }
    public string AssessmentType { get; set; } = "Текуща";
    public DateTime Date { get; set; } = DateTime.Now;

    public Grade() { }

    public Grade(int id, int studentId, int subjectId, int value, string assessmentType)
    {
        Id = id;
        StudentId = studentId;
        SubjectId = subjectId;
        Value = value;
        AssessmentType = assessmentType;
        Date = DateTime.Now;
    }
}
