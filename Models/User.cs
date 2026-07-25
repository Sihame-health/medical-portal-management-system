namespace MedicalSystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Status { get; set; } = "Actif";
        public string LastLogin { get; set; } = "";
        public int? ServiceId { get; set; }
        public string? ServiceName { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}