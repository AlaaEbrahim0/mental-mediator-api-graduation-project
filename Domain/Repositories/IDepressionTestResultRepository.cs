using Domain.Entities;
using Domain.Value_Objects;
using Shared.RequestParameters;

namespace Domain.Repositories;

public interface IDepressionTestResultRepository
{
	Task<IEnumerable<DepressionTestResult>> GetAll(DepressionTestsRequestParameters parameters, bool trackChanges);
	Task<DepressionTestResult?> GetById(int id, bool trackChanges);
	Task<List<DepressionTestResult>?> GetByUserId(string userId, bool trackChanges);
	void CreateDepressionTest(DepressionTestResult test);
	Task<int> GetCount(bool trackChanges);

	Task<Dictionary<string, int>> CalculateResultCounts();
	Task<Dictionary<string, AgeGroupDistribution>> CalculateAgeGroupDistribution();
	Task<Dictionary<string, GenderDistribution>> CalculateGenderDistribution();
	//void DeleteDepressionTest(DepressionTestResult test);
	//void UpdateDepressionTest(DepressionTestResult test);
}