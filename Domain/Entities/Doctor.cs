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
	public List<DoctorAvailabilityPeriod> AvailabilityPeriods { get; set; } = new();
	public List<DoctorTimeOff> TimeOff { get; set; } = new();
	public DoctorSchedulingSettings? SchedulingSettings { get; set; }
}
