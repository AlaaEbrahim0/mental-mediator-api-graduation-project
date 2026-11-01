using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The directory must page deterministically and its result total must match
/// the active filters, otherwise "show more" repeats or skips doctors.
/// </summary>
public class DoctorDirectoryTests : IDisposable
{
	private readonly AppDbContext _context;

	public DoctorDirectoryTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"doctors-{Guid.NewGuid()}")
			.Options;
		_context = new AppDbContext(options);
		Seed();
	}

	public void Dispose() => _context.Dispose();

	private void Seed()
	{
		_context.Doctors.AddRange(
			Doctor("zoe", "Zoe", "Zed", DoctorSpecialization.CounselingPsychology, isAvailable: false, fees: 300m),
			Doctor("amy", "Amy", "Adams", DoctorSpecialization.ClinicalPsychology, isAvailable: true, fees: 100m),
			Doctor("ben", "Ben", "Brown", DoctorSpecialization.ClinicalPsychology, isAvailable: true, fees: 500m),
			Doctor("cy", "Cy", "Cole", DoctorSpecialization.CounselingPsychology, isAvailable: false, fees: 200m));
		_context.SaveChanges();
	}

	private static Doctor Doctor(
		string id,
		string first,
		string last,
		DoctorSpecialization specialization,
		bool isAvailable,
		decimal fees) =>
		new()
		{
			Id = id,
			Email = $"{id}@example.com",
			UserName = id,
			NormalizedEmail = $"{id.ToUpperInvariant()}@EXAMPLE.COM",
			NormalizedUserName = id.ToUpperInvariant(),
			FirstName = first,
			LastName = last,
			Gender = "female",
			Specialization = specialization,
			IsAvailable = isAvailable,
			SessionFees = fees,
			City = "Cairo",
		};

	[Fact]
	public async Task Results_are_ordered_available_first_then_by_name()
	{
		var repository = new DoctorRepository(_context);
		var all = await repository.GetAll(new DoctorRequestParameters { PageNumber = 1, PageSize = 10 }, false);

		all.Select(doctor => doctor.Id).Should().Equal("amy", "ben", "cy", "zoe");
	}

	[Fact]
	public async Task Pages_are_stable_so_no_doctor_is_dropped_or_repeated()
	{
		var repository = new DoctorRepository(_context);
		var firstPage = await repository.GetAll(new DoctorRequestParameters { PageNumber = 1, PageSize = 2 }, false);
		var secondPage = await repository.GetAll(new DoctorRequestParameters { PageNumber = 2, PageSize = 2 }, false);

		firstPage.Select(doctor => doctor.Id).Should().Equal("amy", "ben");
		secondPage.Select(doctor => doctor.Id).Should().Equal("cy", "zoe");
	}

	[Fact]
	public async Task Filtered_count_matches_the_filtered_result_set()
	{
		var repository = new DoctorRepository(_context);
		var request = new DoctorRequestParameters
		{
			Specialization = nameof(DoctorSpecialization.ClinicalPsychology),
			IsAvailable = true,
			MaxFees = 200m,
			PageNumber = 1,
			PageSize = 10,
		};

		var items = await repository.GetAll(request, false);
		var count = await repository.GetFilteredCount(request);

		items.Should().ContainSingle().Which.Id.Should().Be("amy");
		count.Should().Be(1);
	}
}
