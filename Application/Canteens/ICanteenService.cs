using System;
namespace Application.Canteens
{
	public interface ICanteenService
	{
		Task<CanteenResponse> CreateAsync(CreateCanteenRequest request);
		Task<CanteenResponse> GetByIdAsync(string id);
		Task<CanteensResponse> GetCanteensAsync();
		Task<CanteenResponse?> UpdateAsync(string id, UpdateCanteenRequest request);
		Task<bool> DeleteAsync(string id);
		Task<CanteenStatus> GetRemainingCapacityAsync(string canteenId, DateOnly startDate,
			DateOnly endDate, TimeOnly startTime,
			TimeOnly endTime, uint duration);

    }
}

