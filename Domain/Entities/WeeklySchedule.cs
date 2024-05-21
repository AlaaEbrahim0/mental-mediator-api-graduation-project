using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[NotMapped]
public class WeeklySchedule
{
	public List<DoctorScheduleWeekDay> WeekDays { get; set; } = new();
}

