using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

/*
 * Service responsible only for operations related to trips.
 */
public class TripsService : ITripsService
{
    private readonly string _connectionString =
        "Server=localhost,1433;Database=APBD;User Id=sa;Password=ApbdApi123!;Encrypt=False;TrustServerCertificate=True;";

    /*
     * Returns a list of all trips from the database.
     * Each trip includes its basic information and associated countries.
     * Data is grouped by trip ID using a dictionary.
     */
    public async Task<List<TripDTO>> GetTrips()
    {
        var tripsDict = new Dictionary<int, TripDTO>();

        var sql = @"
            SELECT 
                t.IdTrip AS TripId,
                t.Name AS TripName,
                t.Description AS TripDescription,
                t.DateFrom AS DateFrom,
                t.DateTo AS DateTo,
                t.MaxPeople AS MaxPeople,
                c.Name AS CountryName
            FROM Trip t
            LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
            ORDER BY t.IdTrip
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(reader.GetOrdinal("TripId"));

            // If trip not yet in dictionary, create and add it
            if (!tripsDict.ContainsKey(tripId))
            {
                var trip = new TripDTO
                {
                    Id = tripId,
                    Name = reader.GetString(reader.GetOrdinal("TripName")),
                    Description = reader.GetString(reader.GetOrdinal("TripDescription")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    Countries = new List<CountryDTO>()
                };
                tripsDict[tripId] = trip;
            }

            // Add country to the trip if available
            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
            {
                tripsDict[tripId].Countries.Add(new CountryDTO
                {
                    Name = reader.GetString(reader.GetOrdinal("CountryName"))
                });
            }
        }

        return tripsDict.Values.ToList();
    }

    /*
     * Checks whether a trip with the given ID exists.
     * true if the trip exists
     * false if not
     */
    public async Task<bool> TripExists(int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand("SELECT 1 FROM Trip WHERE IdTrip = @IdTrip", connection);
        cmd.Parameters.AddWithValue("@IdTrip", tripId);

        return await cmd.ExecuteScalarAsync() != null;
    }

    /*
     * Returns full trip details for a given trip ID.
     * Includes name, description, dates, max people, and list of countries.
     * If the trip does not exist, returns null.
     */
    public async Task<TripDTO> GetTrip(int tripId)
    {
        TripDTO? trip = null;

        var sql = @"
            SELECT 
                t.IdTrip AS TripId,
                t.Name AS TripName,
                t.Description AS TripDescription,
                t.DateFrom AS DateFrom,
                t.DateTo AS DateTo,
                t.MaxPeople AS MaxPeople,
                c.Name AS CountryName
            FROM Trip t
            LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
            WHERE t.IdTrip = @TripId
        ";

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TripId", tripId);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            // Create trip object only once
            if (trip == null)
            {
                trip = new TripDTO
                {
                    Id = reader.GetInt32(reader.GetOrdinal("TripId")),
                    Name = reader.GetString(reader.GetOrdinal("TripName")),
                    Description = reader.GetString(reader.GetOrdinal("TripDescription")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    Countries = new List<CountryDTO>()
                };
            }

            // Add country if available
            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
            {
                trip.Countries.Add(new CountryDTO
                {
                    Name = reader.GetString(reader.GetOrdinal("CountryName"))
                });
            }
        }

        return trip!;
    }

    /*
     * Checks whether the trip has reached its maximum capacity.
     * true if trip is full
     * false if there are available spots
     */
    public async Task<bool> IsTripFull(int tripId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get max number of people allowed on the trip
        var maxCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip", connection);
        maxCmd.Parameters.AddWithValue("@IdTrip", tripId);
        var maxResult = await maxCmd.ExecuteScalarAsync();
        if (maxResult == null) return false;

        // Get current number of registered clients for the trip
        var currentCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip", connection);
        currentCmd.Parameters.AddWithValue("@IdTrip", tripId);
        var currentCount = (int)await currentCmd.ExecuteScalarAsync();

        return currentCount >= (int)maxResult;
    }
}
