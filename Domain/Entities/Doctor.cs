using Domain.Enums;

namespace Domain.Entities;

public class Doctor : BaseUser
{
	public string Biography { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public decimal SessionFee { get; set; }
	public DoctorSpecialization Specialization { get; set; }
	public List<Appointment> Appointments { get; set; } = new();
	public List<DoctorScheduleWeekDay> DoctorScheduleWeekDays { get; set; } = new();
}


