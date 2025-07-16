using Iot_dashboard.Controllers.GM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json; // Add this namespace for JsonElement
using System.IdentityModel.Tokens.Jwt; // Add this namespace for JwtSecurityTokenHandler
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;


namespace Iot_dashboard.Controllers.GM_API
{
    [ApiController]
    [Route("api/gm/[controller]")]
    public class MeasurementController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public MeasurementController(IConfiguration config, IMemoryCache cache)
        {
            _config = config;
            _cache = cache; // Initialize _cache
        }

        [HttpPost("loadMData")]
        [Authorize]
        public async Task<ActionResult<List<SizeMeasurements>>> LoadMData([FromBody] JsonElement payload) // Change dynamic to JsonElement
        {
            RefreshTokenExpiry();
            string styleName = payload.TryGetProperty("Style", out JsonElement styleProperty) // Explicitly specify JsonElement
                ? styleProperty.GetString()
                : null;
            if (string.IsNullOrEmpty(styleName))
                return BadRequest("Style is required.");

            var result = new List<SizeMeasurements>();
            string connectionString = _config.GetConnectionString("hanger");

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Get style_id from style_code
                var styleIdCmd = new SqlCommand(
                    "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @StyleName", conn);
                styleIdCmd.Parameters.AddWithValue("@StyleName", styleName);

                object styleIdObj = await styleIdCmd.ExecuteScalarAsync();
                if (styleIdObj == null)
                    return NotFound("Style not found.");

                int styleId = (int)styleIdObj;

                // Query measurements for the style using the new structure
                var cmd = new SqlCommand(@"
                SELECT 
                    r.[size], 
                    mt.name AS Measurement, 
                    mt.[type] AS Type, 
                    r.ref_value AS Reference, 
                    r.tolerance_value AS Tolerance
                FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS r
                JOIN CODE.hanger_sys.GM_STYLE_MEASUREMENRTS sm ON r.stylemeas_id = sm.stylemeas_id
                JOIN CODE.hanger_sys.GM_MEASUREMENT_TYPES mt ON sm.measurement_id = mt.measurement_id
                WHERE r.style_id = @StyleId
                ORDER BY r.[size], sm.meas_order ASC", conn);

                cmd.Parameters.AddWithValue("@StyleId", styleId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    string currentSize = null;
                    SizeMeasurements current = null;

                    while (await reader.ReadAsync())
                    {
                        var size = reader["size"].ToString();
                        if (current == null || current.Size != size)
                        {
                            if (current != null)
                                result.Add(current);

                            current = new SizeMeasurements
                            {
                                Size = size,
                                Measurements = new List<MeasurementInfo>()
                            };
                        }

                        current.Measurements.Add(new MeasurementInfo
                        {
                            Measurement = reader["Measurement"].ToString(),
                            Type = reader["Type"].ToString(),
                            Reference = Convert.ToInt32(reader["Reference"]),
                            Tolerance = Convert.ToInt32(reader["Tolerance"])
                        });
                    }
                    if (current != null)
                        result.Add(current);
                }
            }

            return Ok(result);
        }

        [HttpPost("saveMData")]
        [Authorize]
        public async Task<IActionResult> SaveMData([FromBody] JsonElement payload)
        {
            // 1. Parse payload
            RefreshTokenExpiry();
            string session = payload.GetProperty("Session").GetString();
            string style = payload.GetProperty("Style").GetString();
            string size = payload.GetProperty("Size").GetString();
            string soli = payload.GetProperty("SOLI").GetString();
            string epf = payload.GetProperty("EPF").GetRawText();
            string status = payload.GetProperty("Status").GetString();
            string station = payload.GetProperty("Station").GetString();
            string dateTime = payload.GetProperty("DateTime").GetString();
            var measurements = payload.GetProperty("Measurements").EnumerateArray();

            string connectionString = _config.GetConnectionString("hanger");

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 2. Get style_id
                        var styleIdCmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @Style", conn, tran);
                        styleIdCmd.Parameters.AddWithValue("@Style", style);
                        object styleIdObj = await styleIdCmd.ExecuteScalarAsync();
                        if (styleIdObj == null)
                            return NotFound("Style not found.");
                        int styleId = (int)styleIdObj;

                        // 3. Insert into GM_SESSION
                        var insertSessionCmd = new SqlCommand(@"
                    INSERT INTO CODE.hanger_sys.GM_SESSION
                    (style_id, [size], soli, epf, ov_stat, [datetime], station, [session])
                    OUTPUT INSERTED.garment_id
                    VALUES (@StyleId, @Size, @Soli, @Epf, @Status, @DateTime, @Station, @Session)", conn, tran);

                        insertSessionCmd.Parameters.AddWithValue("@StyleId", styleId);
                        insertSessionCmd.Parameters.AddWithValue("@Size", size);
                        insertSessionCmd.Parameters.AddWithValue("@Soli", soli);
                        insertSessionCmd.Parameters.AddWithValue("@Epf", epf);
                        insertSessionCmd.Parameters.AddWithValue("@Status", status);
                        insertSessionCmd.Parameters.AddWithValue("@DateTime", DateTime.Parse(dateTime));
                        insertSessionCmd.Parameters.AddWithValue("@Station", station); // Or get from payload if available
                        insertSessionCmd.Parameters.AddWithValue("@Session", session);

                        long garmentId = (long)await insertSessionCmd.ExecuteScalarAsync();

                        // 4. Insert measurements
                        foreach (var m in measurements)
                        {
                            string mName = m.GetProperty("Measurement").GetString();
                            int value = m.GetProperty("Value").GetInt32();
                            int offset = m.GetProperty("Offset").GetInt32();
                            string mStatus = m.GetProperty("mStatus").GetString();

                            // Get measurement_id
                            var mIdCmd = new SqlCommand(
                                "SELECT measurement_id FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE name = @Name", conn, tran);
                            mIdCmd.Parameters.AddWithValue("@Name", mName);
                            object mIdObj = await mIdCmd.ExecuteScalarAsync();
                            if (mIdObj == null)
                                throw new Exception($"Measurement type '{mName}' not found.");
                            int measurementId = (int)mIdObj;

                            // Insert into GM_MEASUREMENTS
                            var insertMCmd = new SqlCommand(@"
                        INSERT INTO CODE.hanger_sys.GM_MEASUREMENTS
                        (garment_id, measurement_id, value, offset, result)
                        VALUES (@GarmentId, @MeasurementId, @Value, @Offset, @MStatus)", conn, tran);

                            insertMCmd.Parameters.AddWithValue("@GarmentId", garmentId);
                            insertMCmd.Parameters.AddWithValue("@MeasurementId", measurementId);
                            insertMCmd.Parameters.AddWithValue("@Value", value);
                            insertMCmd.Parameters.AddWithValue("@Offset", offset);
                            insertMCmd.Parameters.AddWithValue("@MStatus", mStatus);

                            await insertMCmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(new { GarmentId = garmentId });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, ex.Message);
                    }
                }
            }
        }

        [HttpGet("liveStatus")]
        [Authorize]
        public async Task<IActionResult> GetLiveStatus()
        {
            RefreshTokenExpiry();
            // Get user privilege and plant from JWT claims
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var userPlant = User.Claims.FirstOrDefault(c => c.Type == "plant")?.Value;
            int privilegeLevel = 0;
            if (!string.IsNullOrEmpty(privilegeType))
                int.TryParse(privilegeType, out privilegeLevel);
            var connectionString = _config.GetConnectionString("hanger");
            // No need to query DB for plant
            var result = new LiveStatusResponse
            {
                Latest = new List<LiveStatusEntry>(),
                ALL = new List<LiveStatusEntry>()
            };

            var today = DateTime.Today;

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // 1. Get all stations (tables) with measurements today
                var stationsCmd = new SqlCommand(@"
            SELECT DISTINCT station 
            FROM CODE.hanger_sys.GM_SESSION 
            WHERE CAST([datetime] AS DATE) = @Today
        ", conn);
                stationsCmd.Parameters.AddWithValue("@Today", today);

                var stations = new List<string>();
                using (var reader = await stationsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var station = reader.GetString(0);
                        if (privilegeLevel < 5)
                        {
                            var plantPrefix = station.Split('_')[0];
                            if (!string.Equals(plantPrefix, userPlant, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                        stations.Add(station);
                    }
                }

                List<string>? stationMostFails = null;

                foreach (var station in stations)
                {
                    // 2. Get latest style for this station today
                    var latestStyleCmd = new SqlCommand(@"
                SELECT TOP 1 s.style_code, sess.style_id
                FROM CODE.hanger_sys.GM_SESSION sess
                JOIN CODE.hanger_sys.GM_STYLES s ON sess.style_id = s.style_id
                WHERE sess.station = @Station AND CAST(sess.[datetime] AS DATE) = @Today
                ORDER BY sess.[datetime] DESC
            ", conn);
                    latestStyleCmd.Parameters.AddWithValue("@Station", station);
                    latestStyleCmd.Parameters.AddWithValue("@Today", today);

                    string latestStyle = null;
                    int? latestStyleId = null;
                    using (var reader = await latestStyleCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            latestStyle = reader.GetString(0);
                            latestStyleId = reader.GetInt32(1);
                        }
                    }
                    if (latestStyle == null) continue;

                    // 3. Get all sessions for this style/station today
                    var sessionCmd = new SqlCommand(@"
                SELECT garment_id, ov_stat, [size]
                FROM CODE.hanger_sys.GM_SESSION
                WHERE station = @Station AND style_id = @StyleId AND CAST([datetime] AS DATE) = @Today
            ", conn);
                    sessionCmd.Parameters.AddWithValue("@Station", station);
                    sessionCmd.Parameters.AddWithValue("@StyleId", latestStyleId);
                    sessionCmd.Parameters.AddWithValue("@Today", today);

                    var garmentIds = new List<(long GarmentId, string OvStat, string Size)>();
                    using (var reader = await sessionCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            garmentIds.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
                    }

                    int pass = garmentIds.Count(x => x.OvStat == "Pass");
                    int fail = garmentIds.Count(x => x.OvStat == "Fail");

                    // 4. Get measurement fails and trends for these garments
                    var measurements = new List<MeasurementSummary>();
                    var failCounts = new Dictionary<(string Measurement, string Size), int>();

                    if (garmentIds.Count > 0)
                    {
                        var garmentIdList = string.Join(",", garmentIds.Select(x => x.GarmentId));
                        var measCmd = new SqlCommand($@"
                    SELECT 
                        mt.name, 
                        sess.[size], 
                        COUNT(*) AS Qty, 
                        SUM(CASE WHEN m.[result] = 'Fail' THEN 1 ELSE 0 END) AS FailQty,
                        AVG(m.offset) AS AvgOffset
                    FROM CODE.hanger_sys.GM_MEASUREMENTS m
                    JOIN CODE.hanger_sys.GM_MEASUREMENT_TYPES mt ON m.measurement_id = mt.measurement_id
                    JOIN CODE.hanger_sys.GM_SESSION sess ON m.garment_id = sess.garment_id
                    WHERE m.garment_id IN ({garmentIdList})
                    GROUP BY mt.name, sess.[size]
                ", conn);

                        using (var reader = await measCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var measurement = reader.GetString(0);
                                var size = reader.GetString(1);
                                var failQty = reader.GetInt32(3); // Fail count
                                var avgOffset = reader.IsDBNull(4) ? 0 : Convert.ToInt32(Math.Round(Convert.ToDouble(reader[4]))); // Trend as average offset

                                if (failQty > 0)
                                {
                                    measurements.Add(new MeasurementSummary
                                    {
                                        Measurement = measurement,
                                        Size = size,
                                        Qty = failQty,
                                        Trend = avgOffset
                                    });

                                    failCounts[(measurement, size)] = failQty;
                                }
                            }
                        }
                    }

                    // Update the reference to use stationMostFails instead of mostFailsList
                    if (failCounts.Count > 0)
                    {
                        var maxFail = failCounts.Values.Max();
                        stationMostFails = failCounts
                            .Where(x => x.Value == maxFail)
                            .SelectMany(x => new[] { x.Key.Measurement, x.Key.Size })
                            .ToList();
                    }

                    // Update LiveStatusEntry to use stationMostFails
                    result.Latest.Add(new LiveStatusEntry
                    {
                        Table = station,
                        Style = latestStyle,
                        Pass = pass,
                        Fail = fail,
                        MostFails = stationMostFails,
                        Measurements = measurements
                    });
                }

                // 7. ALL section: all styles/tables measured today
                var allCmd = new SqlCommand(@"
            SELECT sess.station, s.style_code, sess.style_id
            FROM CODE.hanger_sys.GM_SESSION sess
            JOIN CODE.hanger_sys.GM_STYLES s ON sess.style_id = s.style_id
            WHERE CAST(sess.[datetime] AS DATE) = @Today
            GROUP BY sess.station, s.style_code, sess.style_id
        ", conn);
                allCmd.Parameters.AddWithValue("@Today", today);

                var allEntries = new List<(string Station, string Style, int StyleId)>();
                using (var reader = await allCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var station = reader.GetString(0);
                        if (privilegeLevel < 5)
                        {
                            var plantPrefix = station.Split('_')[0];
                            if (!string.Equals(plantPrefix, userPlant, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                        allEntries.Add((station, reader.GetString(1), reader.GetInt32(2)));
                    }
                }

                // For the ALL section, create a separate variable
                List<string>? allMostFails = null;

                foreach (var entry in allEntries)
                {
                    // Repeat similar logic as above for each (station, style)
                    var sessionCmd = new SqlCommand(@"
                SELECT garment_id, ov_stat, [size]
                FROM CODE.hanger_sys.GM_SESSION
                WHERE station = @Station AND style_id = @StyleId AND CAST([datetime] AS DATE) = @Today
            ", conn);
                    sessionCmd.Parameters.AddWithValue("@Station", entry.Station);
                    sessionCmd.Parameters.AddWithValue("@StyleId", entry.StyleId);
                    sessionCmd.Parameters.AddWithValue("@Today", today);

                    var garmentIds = new List<(long GarmentId, string OvStat, string Size)>();
                    using (var reader = await sessionCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            garmentIds.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
                    }

                    int pass = garmentIds.Count(x => x.OvStat == "Pass");
                    int fail = garmentIds.Count(x => x.OvStat == "Fail");

                    var measurements = new List<MeasurementSummary>();
                    var failCounts = new Dictionary<(string Measurement, string Size), int>();

                    if (garmentIds.Count > 0)
                    {
                        var garmentIdList = string.Join(",", garmentIds.Select(x => x.GarmentId));
                        var measCmd = new SqlCommand($@"
                    SELECT 
                        mt.name, 
                        sess.[size], 
                        COUNT(*) AS Qty, 
                        SUM(CASE WHEN m.[result] = 'Fail' THEN 1 ELSE 0 END) AS FailQty,
                        AVG(m.offset) AS AvgOffset
                    FROM CODE.hanger_sys.GM_MEASUREMENTS m
                    JOIN CODE.hanger_sys.GM_MEASUREMENT_TYPES mt ON m.measurement_id = mt.measurement_id
                    JOIN CODE.hanger_sys.GM_SESSION sess ON m.garment_id = sess.garment_id
                    WHERE m.garment_id IN ({garmentIdList})
                    GROUP BY mt.name, sess.[size]
                ", conn);

                        using (var reader = await measCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var measurement = reader.GetString(0);
                                var size = reader.GetString(1);
                                var failQty = reader.GetInt32(3); // Fail count
                                var avgOffset = reader.IsDBNull(4) ? 0 : Convert.ToInt32(Math.Round(Convert.ToDouble(reader[4]))); // Trend as average offset

                                if (failQty > 0)
                                {
                                    measurements.Add(new MeasurementSummary
                                    {
                                        Measurement = measurement,
                                        Size = size,
                                        Qty = failQty,
                                        Trend = avgOffset
                                    });

                                    failCounts[(measurement, size)] = failQty;
                                }
                            }
                        }
                    }

                    if (failCounts.Count > 0)
                    {
                        var maxFail = failCounts.Values.Max();
                        allMostFails = failCounts
                            .Where(x => x.Value == maxFail)
                            .SelectMany(x => new[] { x.Key.Measurement, x.Key.Size })
                            .ToList();
                    }

                    result.ALL.Add(new LiveStatusEntry
                    {
                        Table = entry.Station,
                        Style = entry.Style,
                        Pass = pass,
                        Fail = fail,
                        MostFails = allMostFails,
                        Measurements = measurements
                    });
                }
            }

            return Ok(result);
        }

        [HttpPost("getReport")]
        [Authorize]
        public async Task<IActionResult> GetMeasurementData([FromBody] MeasurementDataRequest request)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var userPlant = User.Claims.FirstOrDefault(c => c.Type == "plant")?.Value;
            int privilegeLevel = 0;
            if (!string.IsNullOrEmpty(privilegeType))
                int.TryParse(privilegeType, out privilegeLevel);
            string connectionString = _config.GetConnectionString("hanger");
            // No need to query DB for plant
            if (string.IsNullOrEmpty(request.Style))
                return BadRequest("Style is required.");

            var resultList = new List<object>();

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Get style_id
                var styleIdCmd = new SqlCommand(
                    "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @Style", conn);
                styleIdCmd.Parameters.AddWithValue("@Style", request.Style);
                object styleIdObj = await styleIdCmd.ExecuteScalarAsync();
                if (styleIdObj == null)
                    return NotFound("Style not found.");
                int styleId = (int)styleIdObj;

                // Build session query
                var sessionQuery = @"
            SELECT garment_id, [size], [datetime], [session], epf, soli, station, ov_stat
            FROM CODE.hanger_sys.GM_SESSION
            WHERE style_id = @StyleId
        ";
                if (!string.Equals(request.Size, "All", StringComparison.OrdinalIgnoreCase))
                    sessionQuery += " AND [size] = @Size";
                if (string.Equals(request.DateFilter, "Yes", StringComparison.OrdinalIgnoreCase))
                    sessionQuery += " AND [datetime] BETWEEN @StartDate AND @EndDate";
                if (privilegeLevel < 5)
                    sessionQuery += " AND LEFT(station, CHARINDEX('_', station) - 1) = @Plant";

                var sessionCmd = new SqlCommand(sessionQuery, conn);
                sessionCmd.Parameters.AddWithValue("@StyleId", styleId);
                if (!string.Equals(request.Size, "All", StringComparison.OrdinalIgnoreCase))
                    sessionCmd.Parameters.AddWithValue("@Size", request.Size);
                if (string.Equals(request.DateFilter, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    sessionCmd.Parameters.AddWithValue("@StartDate", DateTime.Parse(request.StartDate));
                    sessionCmd.Parameters.AddWithValue("@EndDate", DateTime.Parse(request.EndDate));
                }
                if (privilegeLevel < 5)
                    sessionCmd.Parameters.AddWithValue("@Plant", userPlant);

                var sessionDict = new Dictionary<long, dynamic>();
                using (var reader = await sessionCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long garmentId = reader.GetInt64(0);
                        sessionDict[garmentId] = new
                        {
                            GarmentId = garmentId,
                            Size = reader.GetString(1),
                            DateTime = reader.GetDateTime(2),
                            Session = reader.GetString(3),
                            EPF = reader.GetString(4),
                            SOLI = reader.GetString(5),
                            Station = reader.GetString(6),
                            OverallStatus = reader.GetString(7)
                        };
                    }
                }

                if (sessionDict.Count == 0)
                    return Ok(resultList);

                // Get all measurements for these garments
                var garmentIdList = string.Join(",", sessionDict.Keys.OrderBy(x => x));
                var measCmd = new SqlCommand($@"
            SELECT 
                m.measurement_entry_id,
                m.garment_id,
                mt.name AS Measurement,
                m.value,
                m.offset,
                m.result
            FROM CODE.hanger_sys.GM_MEASUREMENTS m
            JOIN CODE.hanger_sys.GM_MEASUREMENT_TYPES mt ON m.measurement_id = mt.measurement_id
            WHERE m.garment_id IN ({garmentIdList})
            ORDER BY m.garment_id ASC, m.measurement_entry_id ASC
        ", conn);

                var measurementsByGarment = new Dictionary<long, List<object>>();
                using (var reader = await measCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long garmentId = reader.GetInt64(1);
                        var measurement = new
                        {
                            measurement = reader.GetString(2),
                            value = reader.GetInt32(3),
                            offset = reader.GetInt32(4),
                            result = reader.GetString(5)
                        };
                        if (!measurementsByGarment.ContainsKey(garmentId))
                            measurementsByGarment[garmentId] = new List<object>();
                        measurementsByGarment[garmentId].Add(measurement);
                    }
                }

                // Build the grouped result
                foreach (var garmentId in sessionDict.Keys.OrderBy(x => x))
                {
                    var session = sessionDict[garmentId];
                    var measurements = measurementsByGarment.ContainsKey(garmentId)
                        ? measurementsByGarment[garmentId]
                        : new List<object>();

                    // Use the lowest measurement_entry_id for this garment as measurementEntryId
                    long measurementEntryId = 0;
                    if (measurementsByGarment.ContainsKey(garmentId) && measurementsByGarment[garmentId].Count > 0)
                    {
                        // You may want to fetch the actual min(measurement_entry_id) if needed
                        // For now, just set to 0 or you can extend the SQL to include it
                        // Or you can add it as a property in the measurement object and get the min here
                    }

                    resultList.Add(new
                    {
                        measurementEntryId = measurementEntryId,
                        garmentId = session.GarmentId,
                        OverallStatus = session.OverallStatus,
                        dateTime = session.DateTime,
                        Session = session.Session,
                        EPF = session.EPF,
                        Size = session.Size,
                        SOLI = session.SOLI,
                        Station = session.Station,
                        Measurements = measurements
                    });
                }
            }

            return Ok(resultList);
        }
        
        [HttpPost("getSizes")]
        [Authorize]
        public async Task<IActionResult> GetSizes([FromBody] JsonElement payload)
        {
            RefreshTokenExpiry();
            if (!payload.TryGetProperty("Style", out var styleElement) || string.IsNullOrEmpty(styleElement.GetString()))
                return BadRequest("Style is required.");

            string styleCode = styleElement.GetString();
            string connectionString = _config.GetConnectionString("hanger");

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Get style_id
                var styleIdCmd = new SqlCommand(
                    "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @StyleCode", conn);
                styleIdCmd.Parameters.AddWithValue("@StyleCode", styleCode);
                object styleIdObj = await styleIdCmd.ExecuteScalarAsync();
                if (styleIdObj == null)
                    return NotFound("Style not found.");
                int styleId = (int)styleIdObj;

                // Get distinct measurable sizes for this style
                var sizesCmd = new SqlCommand(@"
            SELECT DISTINCT [size]
            FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS
            WHERE style_id = @StyleId
            ORDER BY [size] ASC
        ", conn);
                sizesCmd.Parameters.AddWithValue("@StyleId", styleId);

                var sizes = new List<string>();
                using (var reader = await sizesCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        sizes.Add(reader.GetString(0));
                }

                return Ok(sizes);
            }
        }
        private void RefreshTokenExpiry()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = null;
                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch { return; }

                var username = jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(username))
                {
                    var tokenCacheKey = $"token_{username}";
                    if (_cache.TryGetValue(tokenCacheKey, out string cachedToken) && cachedToken == token)
                    {
                        // Reset sliding expiration to 2 hours
                        _cache.Set(tokenCacheKey, token, TimeSpan.FromHours(1));
                    }
                }
            }
        }

    }
}