using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class Children:BaseEntity<int>
    {
        public int ParentId { get; set; }
        public int DoctorId { get; set; }
        public int SpeechLevelId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public DateOnly TherapyStartDate { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public Parent Parent { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public SpeechLevel SpeechLevel { get; set; } = null!;
        public ICollection<ChildProgressAlert> ChildProgressAlerts { get; set; }
        public ICollection<Activity> Activities { get; set; }
    }
}
