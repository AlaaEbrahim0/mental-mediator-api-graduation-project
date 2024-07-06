using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Value_Objects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Repositories;
public class DepressionTestResultRepository : RepositoryBase<DepressionTestResult>,
IDepressionTestResultRepository
{
	public DepressionTestResultRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public void CreateDepressionTest(DepressionTestResult test)
	{
		Create(test);
	}

	public async Task<IEnumerable<DepressionTestResult>> GetAll(DepressionTestsRequestParameters parameters,
		bool trackChanges)
	{
		return await FindAll(trackChanges)
			.OrderByDescending(x => x.Date)
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}

	public async Task<int> GetCount(bool trackChanges)
	{
		return await FindAll(trackChanges)
			.CountAsync();
	}

	public async Task<DepressionTestResult?> GetById(int id, bool trackChanges)
	{
		return await FindByCondition(x => x.Id == id, trackChanges)
			.FirstOrDefaultAsync();
	}

	public async Task<List<DepressionTestResult>?> GetByUserId(string userId, bool trackChanges)
	{
		return await FindByCondition(x => x.UserId == userId, trackChanges)
			.ToListAsync();
	}

	public async Task<Dictionary<string, int>> CalculateResultCounts()
	{

		return await FindAll(false)
			.GroupBy(r => r.Result)
			.Select(g => new
			{
				ResultCategory = g.Key,
				Count = g.Count()
			})
			.ToDictionaryAsync(g => g.ResultCategory, g => g.Count);
	}
	public async Task<Dictionary<string, GenderDistribution>> CalculateGenderDistribution()

	{
		var genderDistribution = await FindAll(false)
			.GroupBy(r => r.Result)
			.Select(g => new
			{
				ResultCategory = g.Key,
				Male = g.Count(r => r.Gender == Gender.male),
				Female = g.Count(r => r.Gender == Gender.female),
				Total = g.Count()
			})
			.ToDictionaryAsync(g => g.ResultCategory, g => new GenderDistribution
			{
				Male = g.Male,
				Female = g.Female,
				Total = g.Total
			});

		return genderDistribution;
	}
	public async Task<Dictionary<string, AgeGroupDistribution>> CalculateAgeGroupDistribution()
	{
		var ageGroupDistribution = await FindAll(false)
			.GroupBy(r => r.Result)
			.Select(g => new
			{
				ResultCategory = g.Key,
				AgeGroup1 = g.Count(r => r.Age <= 20),   // Age 0-20
				AgeGroup2 = g.Count(r => r.Age > 20 && r.Age <= 40), // Age 21-40
				AgeGroup3 = g.Count(r => r.Age > 40)    // Age 41+
			})
			.ToDictionaryAsync(g => g.ResultCategory, g => new AgeGroupDistribution
			{
				AgeGroup1 = g.AgeGroup1,
				AgeGroup2 = g.AgeGroup2,
				AgeGroup3 = g.AgeGroup3
			});

		return ageGroupDistribution;
	}

}
