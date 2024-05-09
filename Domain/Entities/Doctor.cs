namespace Domain.Entities;

public class Doctor : BaseUser
{
	public string Biography { get; set; } = string.Empty;
	//public DoctorSpecialization Specialization { get; set; }
	//public List<Appointment> Appointments { get; set;/**/ } = new();
	//public WeeklySchedule WeeklySchedule { get; set; } = new();
}
