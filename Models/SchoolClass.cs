namespace EduTrack.Models;

public class SchoolClass
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public SchoolClass() { }
    public SchoolClass(int id, string name) { Id = id; Name = name; }
}
