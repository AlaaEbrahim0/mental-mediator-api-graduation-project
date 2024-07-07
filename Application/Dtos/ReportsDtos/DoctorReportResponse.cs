using Domain.Value_Objects;

namespace Application.Dtos.ReportsDtos;

public class DoctorReportResponse
{
    public int TotalAppointments { get; set; }
    public decimal TotalProfit { get; set; }
    public List<WeekdayAppointmentCount> AppointmentsPerWeekday { get; set; } = [];
    public List<AppointmentStatusCount> StatusCounts { get; set; } = [];
    public List<MonthlyAppointmentCount> AppointmentsPerMonth { get; set; } = [];
    public Dictionary<string, int> TestResultsCount { get; set; } = [];
    public Dictionary<string, GenderDistribution> TestResultsGenderDistributions { get; set; } = [];
    public Dictionary<string, AgeGroupDistribution> TestResultsAgeGroupDistributions { get; set; } = [];
}