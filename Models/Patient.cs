using System;

namespace MedicalSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string CIN { get; set; } = "";
        public int Age { get; set; }
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string MedicalHistory { get; set; } = "";
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = "";
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } = "En attente";
        public int RoomNumber { get; set; }
        public int? AssignedNurseId { get; set; }
        public int? AssignedDoctorId { get; set; }

        public string FullName => $"{LastName} {FirstName}";
    }
}