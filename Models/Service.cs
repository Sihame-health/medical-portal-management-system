namespace MedicalSystem.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int DoctorCount { get; set; }
        public string Status { get; set; } = "Actif";
    }
}