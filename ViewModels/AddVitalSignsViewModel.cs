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
    public class AddVitalSignsViewModels
    {
      
            [Required(ErrorMessage = "Patient is required")]
            public int PatientId { get; set; }

            [Required(ErrorMessage = "Temperature is required")]
            [Range(30.0, 45.0, ErrorMessage = "Temperature must be between 30 and 45°C")]
            public double Temperature { get; set; }

            [Required(ErrorMessage = "Heart Rate is required")]
            [Range(30, 250, ErrorMessage = "Heart Rate must be between 30 and 250 bpm")]
            public int HeartRate { get; set; }

            [Required(ErrorMessage = "Blood Pressure is required")]
            [RegularExpression(@"^\d{2,3}/\d{2,3}$", ErrorMessage = "Blood Pressure must be in format 120/80")]
            public string BloodPressure { get; set; } = string.Empty;

            public DateTime RecordedDate { get; set; } = DateTime.Now;
        
    }
}
