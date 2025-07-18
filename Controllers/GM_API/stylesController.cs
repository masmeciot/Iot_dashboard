using Iot_dashboard.Controllers.GM_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json; // Add this namespace for JsonElement
using System.Threading.Tasks; // Add this namespace for Task
using System.IdentityModel.Tokens.Jwt; // Add this namespace for JwtSecurityTokenHandler
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace Iot_dashboard.Controllers.GM_API
{
    [ApiController]
    [Route("api/gm/[controller]")]
    public class stylesController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        public stylesController(IConfiguration config, IMemoryCache cache)
        {
            _config = config;
            _cache = cache; // Initialize _cache
        }

        [HttpPost("getStyles")]
        [Authorize]
        public IActionResult GetStyles()
        {
            RefreshTokenExpiry();
            var styles = new List<string>();
            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT style_code FROM CODE.hanger_sys.GM_STYLES", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            styles.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return Ok(new { Styles = styles });
        }

        [HttpPost("insertStyle")]
        [Authorize]
        public IActionResult InsertStyle([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 3)
            {
                // Return 403 with a JSON message
                return StatusCode(403, new { Message = "Only users with privilege level 3 or above can insert styles." });
            }
            if (!body.TryGetProperty("Style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
            {
                return BadRequest(new { Message = "Missing or invalid 'Style' property." });
            }

            var styleCode = styleElement.GetString();
            if (string.IsNullOrWhiteSpace(styleCode))
            {
                return BadRequest(new { Message = "'Style' cannot be empty." });
            }

            var connectionString = _config.GetConnectionString("hanger");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get next style_id

                    using (var insertCmd = new SqlCommand(
                        "INSERT INTO CODE.hanger_sys.GM_STYLES ( style_code) VALUES ( @code)", connection))
                    {
                        insertCmd.Parameters.AddWithValue("@code", styleCode);

                        insertCmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { Message = "Style inserted successfully." });
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
            {
                return Conflict(new { Message = "Style code already exists." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
            }
        }

        [HttpPost("insertStyleM")]
        [Authorize]
        public async Task<IActionResult> AddStyleMeasurements([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;

            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 2)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 2 or above can insert styles measurements." });
            }

            if (!body.TryGetProperty("Style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'Style' property." });

            var styleCode = styleElement.GetString();
            if (string.IsNullOrWhiteSpace(styleCode))
                return BadRequest(new { Message = "'Style' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // Get style_id
                        int styleId;
                        using (var cmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @code", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@code", styleCode);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Style not found." });
                            styleId = (int)result;
                        }

                        // Determine payload type
                        bool isType1 = body.TryGetProperty("Measurements", out var measurementsElement) &&
                                       measurementsElement.ValueKind == JsonValueKind.Array &&
                                       measurementsElement.GetArrayLength() > 0 &&
                                       measurementsElement[0].ValueKind == JsonValueKind.Object &&
                                       measurementsElement[0].TryGetProperty("Measurement", out _);

                        // Get all existing measurement names for this style
                        var existingMeasurements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var cmd = new SqlCommand(
                            @"SELECT mt.name
                          FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS sm
                          JOIN CODE.hanger_sys.GM_MEASUREMENT_TYPES mt ON sm.measurement_id = mt.measurement_id
                          WHERE sm.style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                    existingMeasurements.Add(reader.GetString(0));
                            }
                        }

                        // Get all sizes for this style from GM_REFERENCE_MEASUREMENTS
                        var sizes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        using (var cmd = new SqlCommand(
                            "SELECT DISTINCT [size] FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                    sizes.Add(reader.GetString(0));
                            }
                        }

                        // For type 1: replace all measurements for the style with the provided list (in order)
                        if (isType1)
                        {
                            // Insert new measurements as needed and add to style
                            foreach (var m in measurementsElement.EnumerateArray())
                            {
                                var mName = m.GetProperty("Measurement").GetString();
                                var mOrder = m.GetProperty("Order").GetInt32();

                                // Get or create measurement type
                                int measurementId;
                                using (var cmd = new SqlCommand(
                                    "SELECT measurement_id FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE name = @name", connection, tran))
                                {
                                    cmd.Parameters.AddWithValue("@name", mName);
                                    var result = await cmd.ExecuteScalarAsync();
                                    if (result == null)
                                    {
                                        // Insert new measurement type
                                        using (var insertCmd = new SqlCommand(
                                            "INSERT INTO CODE.hanger_sys.GM_MEASUREMENT_TYPES (name) OUTPUT INSERTED.measurement_id VALUES (@name)", connection, tran))
                                        {
                                            insertCmd.Parameters.AddWithValue("@name", mName);
                                            measurementId = (int)await insertCmd.ExecuteScalarAsync();
                                        }
                                    }
                                    else
                                    {
                                        measurementId = (int)result;
                                    }
                                }

                                // Insert into GM_STYLE_MEASUREMENRTS if not already present
                                using (var cmd = new SqlCommand(
                                    @"IF NOT EXISTS (
                                    SELECT 1 FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS 
                                    WHERE style_id = @styleId AND measurement_id = @measurementId)
                                  INSERT INTO CODE.hanger_sys.GM_STYLE_MEASUREMENRTS (style_id, measurement_id, meas_order)
                                  VALUES (@styleId, @measurementId, @order)", connection, tran))
                                {
                                    cmd.Parameters.AddWithValue("@styleId", styleId);
                                    cmd.Parameters.AddWithValue("@measurementId", measurementId);
                                    cmd.Parameters.AddWithValue("@order", mOrder);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        else // type 2: append new measurements to the style
                        {
                            var newMeasurements = new List<string>();
                            foreach (var m in measurementsElement.EnumerateArray())
                            {
                                var mName = m.GetString();
                                if (!existingMeasurements.Contains(mName))
                                    newMeasurements.Add(mName);
                            }

                            foreach (var mName in newMeasurements)
                            {
                                // Get or create measurement type
                                int measurementId;
                                using (var cmd = new SqlCommand(
                                    "SELECT measurement_id FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE name = @name", connection, tran))
                                {
                                    cmd.Parameters.AddWithValue("@name", mName);
                                    var result = await cmd.ExecuteScalarAsync();
                                    if (result == null)
                                    {
                                        using (var insertCmd = new SqlCommand(
                                            "INSERT INTO CODE.hanger_sys.GM_MEASUREMENT_TYPES (name) OUTPUT INSERTED.measurement_id VALUES (@name)", connection, tran))
                                        {
                                            insertCmd.Parameters.AddWithValue("@name", mName);
                                            measurementId = (int)await insertCmd.ExecuteScalarAsync();
                                        }
                                    }
                                    else
                                    {
                                        measurementId = (int)result;
                                    }
                                }

                                // Determine next meas_order
                                int nextOrder = 1;
                                using (var cmd = new SqlCommand(
                                    "SELECT ISNULL(MAX(meas_order), 0) + 1 FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE style_id = @styleId", connection, tran))
                                {
                                    cmd.Parameters.AddWithValue("@styleId", styleId);
                                    nextOrder = (int)await cmd.ExecuteScalarAsync();
                                }

                                // Insert into GM_STYLE_MEASUREMENRTS
                                int styleMeasId;
                                using (var cmd = new SqlCommand(
                                    @"INSERT INTO CODE.hanger_sys.GM_STYLE_MEASUREMENRTS (style_id, measurement_id, meas_order)
                                  OUTPUT INSERTED.stylemeas_id
                                  VALUES (@styleId, @measurementId, @order)", connection, tran))
                                {
                                    cmd.Parameters.AddWithValue("@styleId", styleId);
                                    cmd.Parameters.AddWithValue("@measurementId", measurementId);
                                    cmd.Parameters.AddWithValue("@order", nextOrder);
                                    styleMeasId = (int)await cmd.ExecuteScalarAsync();
                                }

                                // For each size, insert a zero-value reference measurement
                                foreach (var size in sizes)
                                {
                                    using (var cmd = new SqlCommand(
                                        @"INSERT INTO CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS
                                      (style_id, [size], stylemeas_id, ref_value, tolerance_value)
                                      VALUES (@styleId, @size, @styleMeasId, 0, 0)", connection, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@styleId", styleId);
                                        cmd.Parameters.AddWithValue("@size", size);
                                        cmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                        return Ok(new { Message = "Measurements added/updated successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
            }
        }

        [HttpPost("insertStyleData")]
        [Authorize]
        public async Task<IActionResult> AddStyleData([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 2)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 2 or above can insert styles measurements." });
            }

            if (!body.TryGetProperty("Style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'Style' property." });
            if (!body.TryGetProperty("Data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
                return BadRequest(new { Message = "Missing or invalid 'Data' property." });

            var styleCode = styleElement.GetString();
            if (string.IsNullOrWhiteSpace(styleCode))
                return BadRequest(new { Message = "'Style' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Get or create style
                        int styleId;
                        using (var cmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @code", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@code", styleCode);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                            {
                                // Insert style
                                using (var insertCmd = new SqlCommand(
                                    "INSERT INTO CODE.hanger_sys.GM_STYLES (style_code) OUTPUT INSERTED.style_id VALUES (@code)", connection, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@code", styleCode);
                                    styleId = (int)await insertCmd.ExecuteScalarAsync();
                                }
                            }
                            else
                            {
                                styleId = (int)result;
                                // Do not rollback or return conflict; proceed to upsert measurements
                            }
                        }

                        // 2. Prepare measurement type and style-measurement caches
                        var measurementTypeCache = new Dictionary<string, (int id, string type, string description)>(StringComparer.OrdinalIgnoreCase);
                        using (var cmd = new SqlCommand(
                            "SELECT measurement_id, name, [type], description FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES", connection, tran))
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var name = reader.GetString(1);
                                    var type = reader.IsDBNull(2) ? null : reader.GetString(2);
                                    var description = reader.IsDBNull(3) ? null : reader.GetString(3);
                                    measurementTypeCache[name] = (reader.GetInt32(0), type, description);
                                }
                            }
                        }

                        // stylemeas: key = measurement_id, value = (stylemeas_id, meas_order)
                        var styleMeasCache = new Dictionary<int, (int stylemeas_id, int meas_order)>();
                        using (var cmd = new SqlCommand(
                            "SELECT stylemeas_id, measurement_id, meas_order FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    styleMeasCache[reader.GetInt32(1)] = (reader.GetInt32(0), reader.GetInt32(2));
                                }
                            }
                        }

                        // 3. For each size in Data
                        foreach (var sizeObj in dataElement.EnumerateArray())
                        {
                            if (!sizeObj.TryGetProperty("size", out var sizeElement) || sizeElement.ValueKind != JsonValueKind.String)
                                continue; // skip invalid size
                            var size = sizeElement.GetString();
                            if (string.IsNullOrWhiteSpace(size))
                                continue;
                            if (!sizeObj.TryGetProperty("measurements", out var measurementsElement) || measurementsElement.ValueKind != JsonValueKind.Array)
                                continue;

                            foreach (var measObj in measurementsElement.EnumerateArray())
                            {
                                if (!measObj.TryGetProperty("measurement", out var measNameElement) || measNameElement.ValueKind != JsonValueKind.String)
                                    continue;
                                var measName = measNameElement.GetString();
                                if (string.IsNullOrWhiteSpace(measName))
                                    continue;
                                var description = measObj.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String ? descElement.GetString() : null;
                                var type = measObj.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : null;
                                var reference = measObj.TryGetProperty("reference", out var refElement) && refElement.ValueKind == JsonValueKind.Number ? refElement.GetInt32() : 0;
                                var tolerance = measObj.TryGetProperty("tolerance", out var tolElement) && tolElement.ValueKind == JsonValueKind.Number ? tolElement.GetInt32() : 0;
                                var order = measObj.TryGetProperty("order", out var orderElement) && orderElement.ValueKind == JsonValueKind.Number ? orderElement.GetInt32() : (int?)null;

                                // 3.1. Get or create measurement type, set description
                                int measurementId;
                                if (!measurementTypeCache.TryGetValue(measName, out var measTypeInfo))
                                {
                                    using (var insertCmd = new SqlCommand(
                                        "INSERT INTO CODE.hanger_sys.GM_MEASUREMENT_TYPES (name, [type], description) OUTPUT INSERTED.measurement_id VALUES (@name, @type, @description)", connection, tran))
                                    {
                                        insertCmd.Parameters.AddWithValue("@name", measName);
                                        insertCmd.Parameters.AddWithValue("@type", (object?)type ?? DBNull.Value);
                                        insertCmd.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
                                        measurementId = (int)await insertCmd.ExecuteScalarAsync();
                                        measurementTypeCache[measName] = (measurementId, type, description);
                                    }
                                }
                                else
                                {
                                    measurementId = measTypeInfo.id;
                                    // Optionally update type or description if changed
                                    bool updateType = !string.Equals(measTypeInfo.type, type, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(type);
                                    bool updateDesc = !string.Equals(measTypeInfo.description, description, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(description);
                                    if (updateType || updateDesc)
                                    {
                                        using (var updateCmd = new SqlCommand(
                                            "UPDATE CODE.hanger_sys.GM_MEASUREMENT_TYPES SET [type] = @type, description = @description WHERE measurement_id = @id", connection, tran))
                                        {
                                            updateCmd.Parameters.AddWithValue("@type", (object?)type ?? DBNull.Value);
                                            updateCmd.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
                                            updateCmd.Parameters.AddWithValue("@id", measurementId);
                                            await updateCmd.ExecuteNonQueryAsync();
                                        }
                                        measurementTypeCache[measName] = (measurementId, type, description);
                                    }
                                }

                                // 3.2. Get or create style-measurement mapping, using 'order' from payload
                                int styleMeasId;
                                if (!styleMeasCache.TryGetValue(measurementId, out var styleMeasInfo))
                                {
                                    int measOrder = order ?? (styleMeasCache.Count > 0 ? styleMeasCache.Values.Max(x => x.meas_order) + 1 : 1);
                                    using (var insertCmd = new SqlCommand(
                                        "INSERT INTO CODE.hanger_sys.GM_STYLE_MEASUREMENRTS (style_id, measurement_id, meas_order) OUTPUT INSERTED.stylemeas_id VALUES (@styleId, @measurementId, @order)", connection, tran))
                                    {
                                        insertCmd.Parameters.AddWithValue("@styleId", styleId);
                                        insertCmd.Parameters.AddWithValue("@measurementId", measurementId);
                                        insertCmd.Parameters.AddWithValue("@order", measOrder);
                                        styleMeasId = (int)await insertCmd.ExecuteScalarAsync();
                                        styleMeasCache[measurementId] = (styleMeasId, measOrder);
                                    }
                                }
                                else
                                {
                                    styleMeasId = styleMeasInfo.stylemeas_id;
                                    // Update meas_order if 'order' is provided and different
                                    if (order.HasValue && order.Value != styleMeasInfo.meas_order)
                                    {
                                        using (var updateCmd = new SqlCommand(
                                            "UPDATE CODE.hanger_sys.GM_STYLE_MEASUREMENRTS SET meas_order = @order WHERE stylemeas_id = @styleMeasId", connection, tran))
                                        {
                                            updateCmd.Parameters.AddWithValue("@order", order.Value);
                                            updateCmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                                            await updateCmd.ExecuteNonQueryAsync();
                                        }
                                        styleMeasCache[measurementId] = (styleMeasId, order.Value);
                                    }
                                }

                                // 3.3. Upsert reference measurement for (style, size, stylemeas_id)
                                int? refId = null;
                                using (var selectCmd = new SqlCommand(
                                    "SELECT ref_id FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE style_id = @styleId AND [size] = @size AND stylemeas_id = @styleMeasId", connection, tran))
                                {
                                    selectCmd.Parameters.AddWithValue("@styleId", styleId);
                                    selectCmd.Parameters.AddWithValue("@size", size);
                                    selectCmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                                    var result = await selectCmd.ExecuteScalarAsync();
                                    if (result != null)
                                        refId = (int)result;
                                }
                                if (refId.HasValue)
                                {
                                    // Update
                                    using (var updateCmd = new SqlCommand(
                                        "UPDATE CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS SET ref_value = @ref, tolerance_value = @tol WHERE ref_id = @refId", connection, tran))
                                    {
                                        updateCmd.Parameters.AddWithValue("@ref", reference);
                                        updateCmd.Parameters.AddWithValue("@tol", tolerance);
                                        updateCmd.Parameters.AddWithValue("@refId", refId.Value);
                                        await updateCmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    // Insert
                                    using (var insertCmd = new SqlCommand(
                                        "INSERT INTO CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS (style_id, [size], stylemeas_id, ref_value, tolerance_value) VALUES (@styleId, @size, @styleMeasId, @ref, @tol)", connection, tran))
                                    {
                                        insertCmd.Parameters.AddWithValue("@styleId", styleId);
                                        insertCmd.Parameters.AddWithValue("@size", size);
                                        insertCmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                                        insertCmd.Parameters.AddWithValue("@ref", reference);
                                        insertCmd.Parameters.AddWithValue("@tol", tolerance);
                                        await insertCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        tran.Commit();
                        return Ok(new { Message = "Style data inserted/updated successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
            }
        }


        [HttpPost("removeStyleM")]
        [Authorize]
        public async Task<IActionResult> RemoveStyleMeasurement([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 2)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 2 or above can remove styles measurements." });
            }

            if (!body.TryGetProperty("Style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'Style' property." });

            if (!body.TryGetProperty("Measurement", out var measElement) || measElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'Measurement' property." });

            var styleCode = styleElement.GetString();
            var measurementName = measElement.GetString();

            if (string.IsNullOrWhiteSpace(styleCode) || string.IsNullOrWhiteSpace(measurementName))
                return BadRequest(new { Message = "'Style' and 'Measurement' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // Get style_id
                        int styleId;
                        using (var cmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @code", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@code", styleCode);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Style not found." });
                            styleId = (int)result;
                        }

                        // Get measurement_id
                        int measurementId;
                        using (var cmd = new SqlCommand(
                            "SELECT measurement_id FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE name = @name", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@name", measurementName);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Measurement type not found." });
                            measurementId = (int)result;
                        }

                        // Get stylemeas_id for this style and measurement
                        int styleMeasId;
                        using (var cmd = new SqlCommand(
                            "SELECT stylemeas_id FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE style_id = @styleId AND measurement_id = @measurementId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            cmd.Parameters.AddWithValue("@measurementId", measurementId);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Measurement type not assigned to this style." });
                            styleMeasId = (int)result;
                        }

                        // Remove reference measurements for this style/measurement
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE style_id = @styleId AND stylemeas_id = @styleMeasId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            cmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Remove style-measurement mapping
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE stylemeas_id = @styleMeasId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleMeasId", styleMeasId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(new { Message = "Measurement type removed from style and references deleted." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
            }
        }
        
        [HttpPost("removeStyle")]
        [Authorize]
        public async Task<IActionResult> RemoveStyle([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            // Check if user is admin
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 2)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 2 or above can remove styles." });
            }

            if (!body.TryGetProperty("Style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'Style' property." });

            var styleCode = styleElement.GetString();
            if (string.IsNullOrWhiteSpace(styleCode))
                return BadRequest(new { Message = "'Style' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // Get style_id
                        int? styleId = null;
                        using (var cmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @code", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@code", styleCode);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Style not found." });
                            styleId = (int)result;
                        }

                        // Get all garment_ids for this style
                        var garmentIds = new List<long>();
                        using (var cmd = new SqlCommand(
                            "SELECT garment_id FROM CODE.hanger_sys.GM_SESSION WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                    garmentIds.Add(reader.GetInt64(0));
                            }
                        }

                        // Delete from GM_MEASUREMENTS
                        if (garmentIds.Count > 0)
                        {
                            using (var cmd = new SqlCommand(
                                "DELETE FROM CODE.hanger_sys.GM_MEASUREMENTS WHERE garment_id IN (" +
                                string.Join(",", garmentIds) + ")", connection, tran))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Delete from GM_SESSION
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_SESSION WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete from GM_REFERENCE_MEASUREMENTS
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete from GM_STYLE_MEASUREMENRTS
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete from GM_STYLES
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_STYLES WHERE style_id = @styleId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(new { Message = "Style and related data removed successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
            }
        }

        [HttpPost("removeMType")]
        [Authorize]
        public async Task<IActionResult> RemoveMeasurementType([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 2)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 2 or above can reomve measurements types." });
            }

            if (!body.TryGetProperty("MeasurementType", out var measElement) || measElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'MeasurementType' property." });

            var measurementName = measElement.GetString();
            if (string.IsNullOrWhiteSpace(measurementName))
                return BadRequest(new { Message = "'MeasurementType' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // Get measurement_id
                        int measurementId;
                        using (var cmd = new SqlCommand(
                            "SELECT measurement_id FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE name = @name", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@name", measurementName);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Measurement type not found." });
                            measurementId = (int)result;
                        }

                        // Get all stylemeas_id for this measurement type
                        var styleMeasIds = new List<int>();
                        using (var cmd = new SqlCommand(
                            "SELECT stylemeas_id FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE measurement_id = @measurementId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@measurementId", measurementId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                    styleMeasIds.Add(reader.GetInt32(0));
                            }
                        }

                        // Remove reference measurements for this measurement type
                        if (styleMeasIds.Count > 0)
                        {
                            using (var cmd = new SqlCommand(
                                "DELETE FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE stylemeas_id IN (" +
                                string.Join(",", styleMeasIds) + ")", connection, tran))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Remove style-measurement mapping
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_STYLE_MEASUREMENRTS WHERE measurement_id = @measurementId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@measurementId", measurementId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Remove measurements for this measurement type
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_MEASUREMENTS WHERE measurement_id = @measurementId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@measurementId", measurementId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Remove the measurement type itself
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES WHERE measurement_id = @measurementId", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@measurementId", measurementId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(new { Message = "Measurement type and all related references and measurements deleted." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
            }
        }

        [HttpGet("getMeasurementTypes")]
        [Authorize]
        public async Task<IActionResult> GetMeasurementTypes()
        {
            RefreshTokenExpiry();
            var connectionString = _config.GetConnectionString("hanger");
            var types = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand("SELECT name, [type] FROM CODE.hanger_sys.GM_MEASUREMENT_TYPES", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            types.Add(new {
                                name = reader.GetString(0),
                                type = reader.IsDBNull(1) ? null : reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return Ok(new { MeasurementTypes = types });
        }

        [HttpPost("deleteSize")]
        [Authorize]
        public async Task<IActionResult> DeleteSize([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 3)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 3 or above can delete sizes." });
            }

            if (!body.TryGetProperty("style", out var styleElement) || styleElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'style' property." });
            if (!body.TryGetProperty("size", out var sizeElement) || sizeElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'size' property." });

            var styleCode = styleElement.GetString();
            var size = sizeElement.GetString();
            if (string.IsNullOrWhiteSpace(styleCode) || string.IsNullOrWhiteSpace(size))
                return BadRequest(new { Message = "'style' and 'size' cannot be empty." });

            var connectionString = _config.GetConnectionString("hanger");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Get style_id
                        int styleId;
                        using (var cmd = new SqlCommand(
                            "SELECT style_id FROM CODE.hanger_sys.GM_STYLES WHERE style_code = @code", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@code", styleCode);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result == null)
                                return NotFound(new { Message = "Style not found." });
                            styleId = (int)result;
                        }

                        // 2. Get all garment_ids for this style and size
                        var garmentIds = new List<long>();
                        using (var cmd = new SqlCommand(
                            "SELECT garment_id FROM CODE.hanger_sys.GM_SESSION WHERE style_id = @styleId AND [size] = @size", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            cmd.Parameters.AddWithValue("@size", size);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                    garmentIds.Add(reader.GetInt64(0));
                            }
                        }

                        // 3. Delete from GM_MEASUREMENTS for these garment_ids
                        if (garmentIds.Count > 0)
                        {
                            using (var cmd = new SqlCommand(
                                "DELETE FROM CODE.hanger_sys.GM_MEASUREMENTS WHERE garment_id IN (" + string.Join(",", garmentIds) + ")", connection, tran))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // 4. Delete from GM_REFERENCE_MEASUREMENTS for this style and size
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_REFERENCE_MEASUREMENTS WHERE style_id = @styleId AND [size] = @size", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            cmd.Parameters.AddWithValue("@size", size);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 5. Delete from GM_SESSION for this style and size
                        using (var cmd = new SqlCommand(
                            "DELETE FROM CODE.hanger_sys.GM_SESSION WHERE style_id = @styleId AND [size] = @size", connection, tran))
                        {
                            cmd.Parameters.AddWithValue("@styleId", styleId);
                            cmd.Parameters.AddWithValue("@size", size);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return Ok(new { Message = "Measurements, reference measurements, and session records for the size deleted successfully." });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
                    }
                }
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
