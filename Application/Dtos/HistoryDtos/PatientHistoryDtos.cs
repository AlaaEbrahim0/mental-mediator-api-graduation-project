namespace Application.Dtos.HistoryDtos;

public class PatientHistoryItemResponse
{
	/// <summary>appointment, note or follow_up.</summary>
	public string Type { get; set; } = string.Empty;

	public DateTime OccurredAtUtc { get; set; }

	public string Title { get; set; } = string.Empty;

	public string? Detail { get; set; }

	public string? Status { get; set; }

	public int? AppointmentId { get; set; }

	public string? DoctorId { get; set; }

	public string? DoctorName { get; set; }

	public string? DoctorPhotoUrl { get; set; }

	public DateTime? DueAtUtc { get; set; }

	/// <summary>For doctor views only; the patient endpoint returns visible records only.</summary>
	public bool IsPatientVisible { get; set; }
}

public class PatientHistoryResponse
{
	public string PatientId { get; set; } = string.Empty;

	public string? PatientName { get; set; }

	public List<PatientHistoryItemResponse> Items { get; set; } = new();

	public int TotalCount { get; set; }

	public int PageNumber { get; set; }

	public int PageSize { get; set; }
}
