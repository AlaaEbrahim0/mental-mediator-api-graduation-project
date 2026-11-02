using FluentValidation;

namespace Application.Dtos.DoctorRatings;

public class UpsertDoctorRatingRequest
{
	public int Stars { get; set; }
	public string? Comment { get; set; }
}

public class UpsertDoctorRatingRequestValidator : AbstractValidator<UpsertDoctorRatingRequest>
{
	public UpsertDoctorRatingRequestValidator()
	{
		RuleFor(x => x.Stars).InclusiveBetween(1, 5);
		RuleFor(x => x.Comment)
			.MaximumLength(1000)
			.When(x => x.Comment is not null);
	}
}

public class DoctorRatingResponse
{
	public int Id { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public int Stars { get; set; }
	public string? Comment { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime? UpdatedAtUtc { get; set; }
}

public class DoctorRatingSummaryResponse
{
	public double? Average { get; set; }
	public int Count { get; set; }
}
