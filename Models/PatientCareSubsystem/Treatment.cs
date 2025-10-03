using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

public class Treatment
{
    [Key]
    public int TreatmentId { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    public string TreatmentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "This field is required.")]
    [DataType(DataType.Date)]
    public DateTime DatePerformed { get; set; }

    public bool IsDeleted { get; set; } = false;

    [ForeignKey(nameof(Patient))]
    public int? PatientId { get; set; }
    public virtual Patient Patient { get; set; }

    public virtual ICollection<TreatmentMedication> TreatmentMedications { get; set; }
}
