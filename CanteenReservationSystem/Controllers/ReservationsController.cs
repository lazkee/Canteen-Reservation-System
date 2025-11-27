using System;
using Microsoft.AspNetCore.Mvc;
using Application.Reservations;

namespace CanteenReservationSystem.Controllers
{
    [Route("reservations")]
    public class ReservationsController : Controller
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _reservationService.CreateAsync(request);

                if (result is null)
                    return BadRequest("Invalid input");

                return CreatedAtAction(nameof(GetReservationById),
                    new { id = result.Id }, result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(string id)
        {
            try
            {
                var result = await _reservationService.GetByIdAsync(id);
                if (result is null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(
            [FromRoute] string id,
            [FromHeader(Name = "studentId")] string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return BadRequest("Missing studentId header.");

            if (!Guid.TryParse(id, out _))
                return BadRequest("Invalid reservation id.");

            if (!Guid.TryParse(studentId, out _))
                return BadRequest("Invalid student id.");

            try
            {
                var result = await _reservationService.CancelAsync(id, studentId);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
