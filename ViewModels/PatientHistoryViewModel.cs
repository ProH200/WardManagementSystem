using System;
using System.Collections.Generic;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.ViewModels
{
    public class PatientHistoryViewModel
    {
        public Patient Patient { get; set; }

        // Change these to List<T> instead of ICollection<T>
        public List<PatientAdmission> AdmissionHistory { get; set; } = new List<PatientAdmission>();
        public List<PatientMovement> MovementHistory { get; set; } = new List<PatientMovement>();
        public List<Discharge> DischargeHistory { get; set; } = new List<Discharge>();

        public PatientAdmission? CurrentAdmission { get; set; }
        public int TotalAdmissions { get; set; }
        public int TotalMovements { get; set; }
        public DateTime? FirstAdmissionDate { get; set; }
        public DateTime? LastAdmissionDate { get; set; }
    }
}