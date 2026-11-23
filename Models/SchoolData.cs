namespace EduTrack.Models;

// Всичко, което съхраняваме, е обединено в един JSON обект.
public class SchoolData
{
    public List<Student> Students { get; set; } = new();
    public List<Grade> Grades { get; set; } = new();
    public List<Absence> Absences { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<SchoolClass> Classes { get; set; } = new();

    public int NextStudentId { get; set; } = 1;
    public int NextGradeId { get; set; } = 1;
    public int NextAbsenceId { get; set; } = 1;
    public int NextSubjectId { get; set; } = 1;
    public int NextClassId { get; set; } = 1;

    public void EnsureCounters()
    {
        Students ??= new(); Grades ??= new(); Absences ??= new();
        Subjects ??= new(); Classes ??= new();
        NextStudentId = Math.Max(NextStudentId, Students.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1);
        NextGradeId = Math.Max(NextGradeId, Grades.Select(g => g.Id).DefaultIfEmpty(0).Max() + 1);
        NextAbsenceId = Math.Max(NextAbsenceId, Absences.Select(a => a.Id).DefaultIfEmpty(0).Max() + 1);
        NextSubjectId = Math.Max(NextSubjectId, Subjects.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1);
        NextClassId = Math.Max(NextClassId, Classes.Select(c => c.Id).DefaultIfEmpty(0).Max() + 1);
    }
}
