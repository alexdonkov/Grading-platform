using System.Text.Json.Serialization;

namespace EduTrack.Models;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int ClassId { get; set; }
    public int Number { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";

    public Student() { } // Нужен на System.Text.Json при зареждане.

    public Student(int id, string firstName, string lastName, int classId, int number)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        ClassId = classId;
        Number = number;
        JoinedAt = DateTime.Now;
    }
}
