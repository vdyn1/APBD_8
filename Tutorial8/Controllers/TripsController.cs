using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    /*
     * This controller handles operations related to trips.
     *
     * Path: /api/Trips
     * [Route("api/[controller]")]  uses controller name "TripsController" without "Controller"  - "Trips"
     */

    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        /*
         * When we call: GET .../api/Trips
         * This method returns the list of all trips.
         * Returns: 200 OK with trip data.
         */
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        /*
         * When we call: GET .../api/Trips/{id}
         * This method returns detailed information about a specific trip by ID.
         *
         * 
         * Check if the trip exists
         * If not return 404 Not Found
         * If yes return 200 OK with trip data
         */
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            if (!await _tripsService.TripExists(id))
            {
                return NotFound($" [ ERROR ] Trip with ID {id} not found.");
            }

            var trip = await _tripsService.GetTrip(id);
            return Ok(trip);
        }
    }
}