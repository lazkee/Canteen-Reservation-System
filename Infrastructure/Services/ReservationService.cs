using System;
using Infrastructure.Data;
using Domain.Models;
using Application.Reservations;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Infrastructure.Services
{
	public class ReservationService : IReservationService
	{
        public readonly CanteenDbContext _context;
		public ReservationService(CanteenDbContext context)
		{
            _context = context;
		}

        

        public async Task<ReservationResponse?> CreateAsync(CreateReservationRequest request)
        {
            //parsing
            if (!Guid.TryParse(request.StudentId, out var studentId))
                return null;

            if (!Guid.TryParse(request.CanteenId, out var canteenId))
                return null;

            if (!DateOnly.TryParse(request.Date, out var date))
                return null;

            if (!TimeOnly.TryParse(request.Time, out var time))
                return null;

            //validation
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            if (date < today)
                return null;

            if (request.Duration is not (30 or 60))
                return null;

            if (time.Minute is not (0 or 30))
                return null;

            var slotStart = time;
            var slotEnd = time.AddMinutes(request.Duration);

            Canteen? canteen = await _context.Canteens.Include(c => c.WorkingHours).
                                    FirstOrDefaultAsync(c => c.CanteenId == canteenId);

            if (canteen is null)
                return null;

            bool fitsWorkingHours = canteen.WorkingHours.Any(wh =>
            slotStart >= wh.StartTime &&
            slotEnd <= wh.EndTime);

            if (!fitsWorkingHours)
                return null;

            bool hasStudentOverlap = await _context.Reservations.AnyAsync(r =>
                    r.StudentId == studentId &&
                    r.Date == date &&
                    r.Status == ReservationStatus.Active &&
                    r.Time < slotEnd &&
                    r.Time.AddMinutes(r.Duration) > slotStart);

            if (hasStudentOverlap)
                return null;

            //available seats check
            var reservedSeats = await _context.Reservations.CountAsync(r =>
            r.CanteenId == canteenId &&
            r.Date == date &&
            r.Status == ReservationStatus.Active &&
            r.Time < slotEnd &&
            r.Time.AddMinutes(r.Duration) > slotStart);

            if (reservedSeats >= canteen.Capacity)
                return null;


            //creation
            var reservation = new Reservation(studentId, canteenId, date, time, request.Duration);

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return new ReservationResponse
            {
                Id = reservation.ReservationId.ToString(),
                Status = reservation.Status.ToString(),
                StudentId = reservation.StudentId.ToString(),
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.Date.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString("HH:mm"),
                Duration = reservation.Duration
            };
        }




        public async Task<ReservationResponse?> CancelAsync(string reservationId, string studentId)
        {
            if (!Guid.TryParse(reservationId, out var resId))
                return null;

            if (!Guid.TryParse(studentId, out var studentGuid))
                return null;

            var reservation = await _context.Reservations.FindAsync(resId);
            if (reservation is null)
                return null;

            if (reservation.StudentId != studentGuid)
                return null;

            if (reservation.Status != ReservationStatus.Cancelled)
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

            return new ReservationResponse
            {
                Id = reservation.ReservationId.ToString(),
                Status = reservation.Status.ToString(),
                StudentId = reservation.StudentId.ToString(),
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.Date.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString("HH:mm"),
                Duration = reservation.Duration
            };
        }


        public async Task<ReservationResponse?> GetByIdAsync(string reservationId)
        {
            if (!Guid.TryParse(reservationId, out var id))
                return null;

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation is null)
                return null;

            return new ReservationResponse
            {
                Id = reservation.ReservationId.ToString(),
                Status = reservation.Status.ToString(),
                StudentId = reservation.StudentId.ToString(),
                CanteenId = reservation.CanteenId.ToString(),
                Date = reservation.Date.ToString("yyyy-MM-dd"),
                Time = reservation.Time.ToString("HH:mm"),
                Duration = reservation.Duration
            };
        }

    }
}

