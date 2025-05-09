using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    /*
     * This controller is responsible for handling all operations 
     * related to clients and their participation in trips.
     *
     *  Path: /api/Clients
     *  [Route("api/[controller]")]  uses controller name "ClientsController"
     *                                      without "Controller" -  "Clients"
     */
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ITripsService _tripsService;

        public ClientsController(IClientService clientService, ITripsService tripsService)
        {
            _clientService = clientService;
            _tripsService = tripsService;
        }

        /*
         * When we call: GET .../api/Clients
         * This method will return the list of all clients from the database.
         * returns 200 OK with client data.
         */
        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _clientService.GetClients();
            return Ok(clients);
        }

        /*
         * When we call: GET .../api/Clients/{id}
         * This method will return specific client info by ID.
         *
         * Before returning, we check if the client exists.
         * Ifn not  returns 404 Not Found.
         * If found  returns 200 OK with client data.
         */
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClient(int id)
        {
            if (!await _clientService.ClientExists(id))
            {
                return NotFound($"[ ERROR ] Client with ID {id} not found.");
            }

            var client = await _clientService.GetClient(id);
            return Ok(client);
        }

        /*
         * When we call: GET .../api/Clients/{clientId}/trips
         * This method returns all trips assigned to the given client.
         *
         * First it checks if the client exists.
         * If not  returns 404 Not Found.
         * If yes  returns 200 OK with trips list.
         */
        [HttpGet("{clientId}/trips")]
        public async Task<IActionResult> GetTripsForClient(int clientId)
        {
            if (!await _clientService.ClientExists(clientId))
            {
                return NotFound($"[ ERROR ] Client with ID {clientId} not found.");
            }

            var trips = await _clientService.GetTripsForClient(clientId);
            return Ok(trips);
        }

        /*
         * When we call: PUT .../api/Clients/{clientId}/trips/{tripId}
         * This method assigns the client to a trip.
         *
         * 
         *Check if client exists:   if not  return 404
         *Check if trip exists: if not  return 404
         *Check if client is already registered : if yes : return 409 Conflict
         *Check if trip is full:  if yes  return 400 Bad Request
         *
         * If all checks pass → client is added to the trip → return 201 Created
         */
        [HttpPut("{clientId}/trips/{tripId}")]
        public async Task<IActionResult> AddTrip(int clientId, int tripId)
        {
            if (!await _clientService.ClientExists(clientId))
            {
                return NotFound($"[ ERROR ] Client with ID {clientId} not found.");
            }

            if (!await _tripsService.TripExists(tripId))
            {
                return NotFound($"[ ERROR ] Trip with ID {tripId} not found.");
            }

            if (await _clientService.IsClientRegisteredForTrip(clientId, tripId))
            {
                return Conflict($"[ ERROR ] Client with ID {clientId} is already registered for trip {tripId}.");
            }

            if (await _tripsService.IsTripFull(tripId))
            {
                return BadRequest($"[ ERROR ] Trip with ID {tripId} is already full.");
            }

            await _clientService.AddClientToTheTrip(clientId, tripId);
            return Created();
        }

        /*
         * When we call: DELETE .../api/Clients/{clientId}/trips/{tripId}
         * This method removes the client from a trip.
         *
         *
         * Check if client exists:  if not  return 404
         * Check if trip exists: if not  return 404
         * eck if client is registered for this trip:  if not  return 400
         *
         * If all checks pass  remove the registration : return 204 No Content
         */
        [HttpDelete("{clientId}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientFromTrip(int clientId, int tripId)
        {
            if (!await _clientService.ClientExists(clientId))
            {
                return NotFound($"[ ERROR ] Client with ID {clientId} not found.");
            }

            if (!await _tripsService.TripExists(tripId))
            {
                return NotFound($"[ ERROR ] Trip with ID {tripId} not found.");
            }

            if (!await _clientService.IsClientRegisteredForTrip(clientId, tripId))
            {
                return BadRequest($"[ ERROR ] Client with ID {clientId} is not registered for trip {tripId}.");
            }

            await _clientService.DeleteClientTripRegistration(clientId, tripId);
            return NoContent();
        }
    }
}
