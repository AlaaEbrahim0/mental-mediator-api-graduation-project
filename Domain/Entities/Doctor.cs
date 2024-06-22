using Domain.Enums;

namespace Domain.Entities;

public class Doctor : BaseUser
{
	public string? Biography { get; set; }
	public string? City { get; set; }
	public string? Location { get; set; }
	public decimal SessionFees { get; set; }
	public DoctorSpecialization Specialization { get; set; }
	public List<Appointment> Appointments { get; set; } = new();
	public List<DoctorScheduleWeekDay> DoctorScheduleWeekDays { get; set; } = new();
}

