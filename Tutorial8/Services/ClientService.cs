using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

/*
 * Service responsible only for operations related to clients.
 */
public class ClientService : IClientService
{
    private readonly string _connectionString =
        "Server=localhost,1433;Database=APBD;User Id=sa;Password=ApbdApi123!;Encrypt=False;TrustServerCertificate=True;";

    /*
     * Checks if a client with the given ID exists in the database.
     * 
     * true if client exists
     * false  if client not found
     */
    public async Task<bool> ClientExists(int clientId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient", connection);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        return await cmd.ExecuteScalarAsync() != null;
    }

    /*
     * Returns a list of all clients from the database.
     */
    public async Task<List<ClientDTO>> GetClients()
    {
        var clients = new List<ClientDTO>();

        var sql = @"
            SELECT IdClient, FirstName, LastName, Email, Telephone, Pesel
            FROM Client
        ";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            clients.Add(new ClientDTO
            {  
                /*
                 * Maps the result set to a list of ClientDTO objects.
                 */
                IdClient = reader.GetInt32(reader.GetOrdinal("IdClient")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Telephone = reader.IsDBNull(reader.GetOrdinal("Telephone"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Telephone")),
                Pesel = reader.IsDBNull(reader.GetOrdinal("Pesel"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Pesel"))
            });
        }

        return clients;
    }

    /*
     * Returns information about a specific client by ID.
     * If the client does not exist, returns null.
     */
    public async Task<ClientDTO?> GetClient(int id)
    {
        ClientDTO? client = null;

        var sql = @"
            SELECT IdClient, FirstName, LastName, Email, Telephone, Pesel
            FROM Client
            WHERE IdClient = @IdClient
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", id);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            client = new ClientDTO
            {
                IdClient = reader.GetInt32(reader.GetOrdinal("IdClient")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Telephone = reader.IsDBNull(reader.GetOrdinal("Telephone"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Telephone")),
                Pesel = reader.IsDBNull(reader.GetOrdinal("Pesel"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Pesel"))
            };
        }

        return client;
    }

    /*
     * Returns a list of trips the specified client is registered for.
     * Includes basic trip info, registration date, payment date, and visited countries.
     */
    public async Task<List<ClientTripDTO>> GetTripsForClient(int clientId)
    {
        var clientTripsDict = new Dictionary<int, ClientTripDTO>();

        var sql = @"
            SELECT 
                t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                ct.RegisteredAt, ct.PaymentDate,
                ctry.Name AS CountryName
            FROM Trip t
            JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
            JOIN Client c ON c.IdClient = ct.IdClient
            LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
            LEFT JOIN Country ctry ON ctr.IdCountry = ctry.IdCountry
            WHERE c.IdClient = @IdClient
        ";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

            if (!clientTripsDict.ContainsKey(tripId))
            {
                var tripDto = new ClientTripDTO
                {
                    IdTrip = tripId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                    PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("PaymentDate")),
                    Countries = new List<string>()
                };

                clientTripsDict[tripId] = tripDto;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
            {
                clientTripsDict[tripId].Countries.Add(reader.GetString(reader.GetOrdinal("CountryName")));
            }
        }

        return clientTripsDict.Values.ToList();
    }

    /*
     * Adds a new registration for the given client and trip.
     * Sets RegisteredAt to current date (format: yyyyMMdd).
     * PaymentDate is set to NULL initially.
     */
    public async Task AddClientToTheTrip(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var insertCmd = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
            VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)", connection);

        insertCmd.Parameters.AddWithValue("@IdClient", clientId);
        insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
        insertCmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));

        await insertCmd.ExecuteNonQueryAsync();
    }

    /*
     * Checks whether the client is already registered for the given trip.
     * true  if registered
     * false  if not registered
     */
    public async Task<bool> IsClientRegisteredForTrip(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);

        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);

        return await cmd.ExecuteScalarAsync() != null;
    }

    /*
     * Removes the registration of a client from a specific trip.
     */
    public async Task DeleteClientTripRegistration(int clientId, int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", connection);

        cmd.Parameters.AddWithValue("@IdClient", clientId);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);

        await cmd.ExecuteNonQueryAsync();
    }
}
