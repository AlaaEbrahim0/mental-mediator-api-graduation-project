namespace Domain.Entities;

public class User : BaseUser
{
	public List<Appointment>? Appointments { get; set; }
}
