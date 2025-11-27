using System;
namespace Application.Reservations
{
	public interface IReservationService
	{
        Task<ReservationResponse?> CreateAsync(CreateReservationRequest request);

        Task<ReservationResponse?> CancelAsync(string reservationId, string studentId);
        Task<ReservationResponse?> GetByIdAsync(string reservationId);

    }
}

