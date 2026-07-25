using System;
using System.Collections.Generic;

namespace MedicalSystem.Models
{
    public class Prescription
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "";
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = "";
        public DateTime CreationDate { get; set; }
        public string Status { get; set; } = "Créée";
        public List<PrescriptionItem> Medications { get; set; } = new List<PrescriptionItem>();
        public string Notes { get; set; } = "";

        public class PrescriptionItem
        {
            public string MedicationName { get; set; } = ""; // Juste le nom: "Paracétamol"
            public string Dosage { get; set; } = "";        // "500mg"
            public string Frequency { get; set; } = "";     // "2x/jour"
            public string Duration { get; set; } = "";      // "5 jours"
            public int Quantity { get; set; }               // Quantité demandée
        }
    }
}