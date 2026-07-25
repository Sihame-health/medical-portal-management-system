using System;

namespace MedicalSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RelatedPrescriptionId { get; set; }
        public int? RelatedPatientId { get; set; }
        public int? SenderId { get; set; } // Nouveau: ID de l'utilisateur qui envoie
        public string SenderName { get; set; } = ""; // Nouveau: Nom de l'envoyeur
        public bool IsUrgent { get; set; } // Nouveau: Pour les notifications urgentes

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                if (timeSpan.TotalMinutes < 1) return "À l'instant";
                if (timeSpan.TotalHours < 1) return $"Il y a {timeSpan.Minutes} min";
                if (timeSpan.TotalDays < 1) return $"Il y a {timeSpan.Hours} h";
                return $"Il y a {timeSpan.Days} j";
            }
        }
    }
}