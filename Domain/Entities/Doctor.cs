using Domain.Enums;

namespace Domain.Entities;

public class Doctor : BaseUser
{
	public string? Location { get; set; }
	public string? Qualifications { get; set; }
	public bool IsAvailable { get; set; } = true;
	public decimal SessionFees { get; set; }
	public DoctorSpecialization Specialization { get; set; }
	public List<Appointment> Appointments { get; set; } = new();
	public List<DoctorScheduleWeekDay> DoctorScheduleWeekDays { get; set; } = new();
	public List<DoctorRecurringBreak> RecurringBreaks { get; set; } = new();
	public List<DoctorAvailabilityOverride> AvailabilityOverrides { get; set; } = new();
	public List<DoctorTimeBlock> TimeBlocks { get; set; } = new();
	public DoctorSchedulingSettings? SchedulingSettings { get; set; }
}
