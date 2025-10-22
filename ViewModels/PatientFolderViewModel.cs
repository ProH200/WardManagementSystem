using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.ViewModels
{
    public class PatientFolderViewModel
    {
        public PatientAdmission Admission { get; set; }
        public List<PatientMedicalHistory> MedicalHistories { get; set; } = new();
        public List<Allergy> Allergies { get; set; }
        public List<MedicalCondition> MedicalConditions { get; set; }
        public Patient Patient { get; set; }
        public List<VitalSigns> VitalSigns { get; set; }
        public List<Treatment> Treatments { get; set; }
        public List<DoctorVisit> DoctorVisits { get; set; }
        public List<Prescription> Prescriptions { get; set; }
        public List<Medication> Medications { get; set; } = new List<Medication>();

        // Add these new properties
        public List<Medication> NonScheduledMedications { get; set; } = new List<Medication>();
        public List<Medication> ScheduledMedications { get; set; } = new List<Medication>();
        public List<Prescription> NonScheduledPrescriptions { get; set; } = new List<Prescription>();
        public List<Prescription> ScheduledPrescriptions { get; set; } = new List<Prescription>();

        public VitalSigns NewVitalSign { get; set; } = new VitalSigns();
        public Treatment NewTreatment { get; set; } = new Treatment();
        public DoctorVisit NewDoctorVisit { get; set; } = new DoctorVisit();
        public Prescription NewPrescription { get; set; } = new Prescription();
        //public Allergy NewAllergy { get; set; } = new Allergy();
        //public MedicalCondition NewMedicalCondition { get; set; } = new MedicalCondition();

        public List<PatientAdmission> Admissions { get; set; } = new List<PatientAdmission>();
        public PatientAdmission CurrentAdmission { get; set; } // Currently active admission
        public PatientAdmission NewAdmission { get; set; } = new PatientAdmission();

        // Add admission form properties
        public bool IsCurrentlyAdmitted { get; set; }
        public DateTime? DischargeDate { get; set; }
        public List<VisitNote> VisitNotes { get; set; } = new List<VisitNote>();
    }
}