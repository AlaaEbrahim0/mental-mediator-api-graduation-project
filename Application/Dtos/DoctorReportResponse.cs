using Domain.Value_Objects;

namespace Application.Dtos;


public class DoctorReportResponse
{
	public int TotalAppointments { get; set; }
	public decimal TotalProfit { get; set; }
	public List<WeekdayAppointmentCount> AppointmentsPerWeekday { get; set; } = [];
	public List<AppointmentStatusCount> StatusCounts { get; set; } = [];
	public List<MonthlyAppointmentCount> AppointmentsPerMonth { get; set; } = [];
}