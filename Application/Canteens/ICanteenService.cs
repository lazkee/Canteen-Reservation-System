using System;
namespace Application.Canteens
{
	public interface ICanteenService
	{
		Task<CanteenResponse> CreateAsync(CreateCanteenRequest request);
		Task<CanteenResponse> GetByIdAsync(string id);
		Task<List<CanteenResponse>> GetCanteensAsync();
        Task<CanteenResponse?> UpdateAsync(string id, UpdateCanteenRequest request);
		Task<bool> DeleteAsync(string id);
		Task<List<CanteenStatus>> GetRemainingCapacityAsync( DateOnly startDate,
			DateOnly endDate, TimeOnly startTime,
			TimeOnly endTime, uint duration);
        Task<CanteenStatus?> GetRemainingCapacityForCanteenAsync(string canteenId,
			DateOnly startDate,DateOnly endDate,TimeOnly startTime,
			TimeOnly endTime,uint duration);

    }
}

