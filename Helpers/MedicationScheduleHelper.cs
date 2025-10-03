using Microsoft.AspNetCore.Mvc.Rendering;

public static class MedicationScheduleHelper
{
    public static readonly Dictionary<int, string> ScheduleDescriptions = new()
    {
        { 1, "Schedule 1 - Simple OTC medications" },
        { 2, "Schedule 2 - Pharmacy-only (mild)" },
        { 3, "Schedule 3 - Pharmacist-prescribed" },
        { 4, "Schedule 4 - Prescription-only medicines" },
        { 5, "Schedule 5 - Controlled substances (Nursing Sister only)" },
        { 6, "Schedule 6 - Highly controlled substances" },
        { 7, "Schedule 7 - Extremely dangerous substances" }
    };

    public static List<SelectListItem> GetScheduleOptions()
    {
        return ScheduleDescriptions.Select(s => new SelectListItem
        {
            Value = s.Key.ToString(),
            Text = $"Schedule {s.Key} - {s.Value.Split('-')[1].Trim()}"
        }).ToList();
    }

    public static string GetScheduleDescription(int scheduleLevel)
    {
        return ScheduleDescriptions.ContainsKey(scheduleLevel)
            ? ScheduleDescriptions[scheduleLevel]
            : "Unknown Schedule";
    }

    public static bool CanNurseDispense(int scheduleLevel)
    {
        return scheduleLevel <= 4; // Nurses can only dispense schedules 1-4
    }

    public static bool RequiresNursingSister(int scheduleLevel)
    {
        return scheduleLevel >= 5; // Schedules 5+ require Nursing Sister
    }
}