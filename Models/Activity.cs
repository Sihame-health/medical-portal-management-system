using System;

namespace MedicalSystem.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
    }
}