using Domain.Value_Objects;

namespace Application.Dtos.ReportsDtos;

public class AdminReportResponse
{
    public int TotalAppointmentsCount { get; set; }
    public int TotalPostCount { get; set; }
    public int TotalUserCount { get; set; }
    public int TotalDoctorCount { get; set; }
    public Dictionary<string, int> TestResultsCount { get; set; } = [];
    public Dictionary<string, GenderDistribution> TestResultsGenderDistributions { get; set; } = [];
    public Dictionary<string, AgeGroupDistribution> TestResultsAgeGroupDistributions { get; set; } = [];

}