using Domain.Entities;

namespace Domain.Repositories;

public interface IAvailableDayRepository
{
	Task<List<AvailableDays>> GetAvailableDays(int scheduleId, bool trackChanges);
	Task<AvailableDays?> GetAvailableDay(int scheduleId, int availableDayId, bool trackChanges);
	void UpdateAvailableDay(AvailableDays AvailableDays);
	void CreateAvailableDay(AvailableDays AvailableDays);
	void DeleteAvailableDay(AvailableDays AvailableDays);
}