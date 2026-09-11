// ViewModels/PatientManagementDashboardViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.Models.DoctorPatientSubsystem;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;

namespace Wellness_Wardens_Project.ViewModels
{
    public class PatientManagementDashboardViewModel
    {
        // ==============================
        // STATISTICS
        // ==============================
        public int TotalPatients { get; set; }
        public int TodayVisits { get; set; }
        public int ActiveMedications { get; set; }
        public int PendingTreatments { get; set; }
        public int NewPatientsThisWeek { get; set; }
        public int VitalsDueToday { get; set; }

        // ==============================
        // LISTS
        // ==============================
        public List<PatientDashboardItemViewModel> Patients { get; set; } = new List<PatientDashboardItemViewModel>();
        public List<VitalSignsDashboardItemViewModel> VitalSigns { get; set; } = new List<VitalSignsDashboardItemViewModel>();
        public List<TreatmentDashboardItemViewModel> Treatments { get; set; } = new List<TreatmentDashboardItemViewModel>();
        public List<MedicationAssignmentDashboardItemViewModel> MedicationAssignments { get; set; } = new List<MedicationAssignmentDashboardItemViewModel>();
        public List<PrescriptionDashboardItemViewModel> Prescriptions { get; set; } = new List<PrescriptionDashboardItemViewModel>();
        public List<DoctorVisitDashboardItemViewModel> DoctorVisits { get; set; } = new List<DoctorVisitDashboardItemViewModel>();

        // ==============================
        // DROPDOWN DATA
        // ==============================
        public List<PatientSelectViewModel> PatientSelectList { get; set; } = new List<PatientSelectViewModel>();
        public List<MedicationSelectViewModel> MedicationSelectList { get; set; } = new List<MedicationSelectViewModel>();
        public List<string> TreatmentTypes { get; set; } = new List<string>();
    }

    // ==============================
    // PATIENT DASHBOARD ITEM
    // ==============================
    public class PatientDashboardItemViewModel
    {
        public int PatientId { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public int Age => DateTime.Now.Year - DateOfBirth.Year;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public DateTime? LastVisit { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ==============================
    // VITAL SIGNS DASHBOARD ITEM
    // ==============================
    public class VitalSignsDashboardItemViewModel
    {
        public int VitalId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public int HeartRate { get; set; }
        public string BloodPressure { get; set; } = string.Empty;
        public DateTime RecordedDate { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
    }

    // ==============================
    // TREATMENT DASHBOARD ITEM
    // ==============================
    public class TreatmentDashboardItemViewModel
    {
        public int TreatmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TreatmentType { get; set; } = string.Empty;
        public DateTime DatePerformed { get; set; }
        public bool IsDeleted { get; set; }
    }

    // ==============================
    // MEDICATION ASSIGNMENT DASHBOARD ITEM
    // ==============================
    public class MedicationAssignmentDashboardItemViewModel
    {
        public int PatientMedicationId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int DurationDays { get; set; }
        public int QuantityAssigned { get; set; }
        public DateTime AssignmentDate { get; set; }
        public int ScheduleLevel { get; set; }
        public bool IsScheduled { get; set; }
        public bool IsActive { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string AdministrationNotes { get; set; } = string.Empty;
    }

    // ==============================
    // PRESCRIPTION DASHBOARD ITEM
    // ==============================
    public class PrescriptionDashboardItemViewModel
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime DateWritten { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public List<PrescriptionMedicationDashboardItemViewModel> Medications { get; set; } = new List<PrescriptionMedicationDashboardItemViewModel>();
    }

    public class PrescriptionMedicationDashboardItemViewModel
    {
        public int MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
    }

    // ==============================
    // DOCTOR VISIT DASHBOARD ITEM
    // ==============================
    public class DoctorVisitDashboardItemViewModel
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Instructions { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
    }

    // ==============================
    // DROPDOWN VIEW MODELS
    // ==============================
    public class PatientSelectViewModel
    {
        public int PatientId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string IdentityNumber { get; set; } = string.Empty;
        public string DisplayName => $"{FirstName} {LastName} ({IdentityNumber})";
    }

    public class MedicationSelectViewModel
    {
        public int MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public int ScheduleLevel { get; set; }
        public bool IsScheduledMedication { get; set; }
        public string DisplayName => $"{Name} (Available: {QuantityAvailable}) - Level {ScheduleLevel}";
    }

    // ==============================
    // FORM VIEW MODELS
    // ==============================
    public class AddVitalSignsViewModel
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Temperature is required")]
        public double Temperature { get; set; }

        [Required(ErrorMessage = "Heart Rate is required")]
        public int HeartRate { get; set; }

        [Required(ErrorMessage = "Blood Pressure is required")]
        [RegularExpression(@"^\d{2,3}/\d{2,3}$", ErrorMessage = "Blood Pressure must be in format 120/80")]
        public string BloodPressure { get; set; } = string.Empty;

        public DateTime RecordedDate { get; set; } = DateTime.Now;
    }

    public class AddTreatmentViewModel
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Treatment Type is required")]
        public string TreatmentType { get; set; } = string.Empty;

        public DateTime DatePerformed { get; set; } = DateTime.Now;
    }

    public class AssignMedicationViewModel
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Medication is required")]
        public int MedicationId { get; set; }

        [Required(ErrorMessage = "Dosage is required")]
        public string Dosage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Frequency is required")]
        public string Frequency { get; set; } = string.Empty;

        [Range(0, 365, ErrorMessage = "Duration must be between 0 and 365 days")]
        public int DurationDays { get; set; }

        [Range(1, 1000, ErrorMessage = "Quantity must be at least 1")]
        public int QuantityAssigned { get; set; } = 1;

        public string AdministrationNotes { get; set; } = string.Empty;
    }

    public class AddDoctorVisitViewModel
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Visit Date is required")]
        public DateTime VisitDate { get; set; } = DateTime.Now;

        public string Instructions { get; set; } = string.Empty;
    }
}