using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem
{
    public class AssignConditionsViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public List<int>? SelectedAllergyIds { get; set; }
        public List<int>? SelectedMedicalConditionIds { get; set; }

        public List<Allergy> AvailableAllergies { get; set; } = new List<Allergy>();
        public List<MedicalCondition> AvailableMedicalConditions { get; set; } = new List<MedicalCondition>();

        public List<Allergy> CurrentPatientAllergies { get; set; } = new List<Allergy>();
        public List<MedicalCondition> CurrentPatientConditions { get; set; } = new List<MedicalCondition>();
    }
}
