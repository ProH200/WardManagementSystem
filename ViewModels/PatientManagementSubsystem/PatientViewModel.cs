namespace Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem
{
    public class PatientViewModel
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string ChronicCondition { get; set; } = string.Empty;
        public string ChronicMedication { get; set; } = string.Empty;
        public string CurrentLocation { get; set; }    // Ward / Room / Bed
        public string AssignedEmployee { get; set; }
    }
}
