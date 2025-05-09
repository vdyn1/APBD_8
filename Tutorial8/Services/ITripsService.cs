using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();

    Task<bool> TripExists(int TripId);

    Task<TripDTO> GetTrip(int id);
    Task<bool> IsTripFull(int tripId);
}