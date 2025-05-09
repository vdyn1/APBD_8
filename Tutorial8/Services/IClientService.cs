using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<bool> ClientExists(int clientId);
    Task<List<ClientDTO>> GetClients();
    Task<ClientDTO> GetClient(int id);
    
    Task<List<ClientTripDTO>> GetTripsForClient(int clientId);
    
    Task AddClientToTheTrip(int clientId, int tripId);
    Task<bool> IsClientRegisteredForTrip(int clientId, int tripId);
    Task DeleteClientTripRegistration(int clientId, int tripId);
}