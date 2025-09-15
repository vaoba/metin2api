using metin2freebsdapi.Endpoints;
using metin2freebsdapi.Helpers;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddMemoryCache();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.AddWebsiteEndpoints();

// app.MapPost("/notice", async (req req) =>
// {
//     try
//     {
//         var reply = await ServerHelpers.SendCommandAsync(["metin2adminpass", req.message]);
//         return Results.Json(new { ok = true, reply });
//     }
//     catch (Exception ex)
//     {
//         return Results.Json(new { ok = false, reply = ex.Message });
//     }
// });
//
// app.MapPost("/create-account", async (CreateAccountRequest req, ILogger<Program> logger) =>
// {
//     logger.LogInformation("account information received: {Login}", req.Login);
//     await using var conn = new MySqlConnection(connectionString);
//     await conn.OpenAsync();
//     var cmd = new MySqlCommand("INSERT INTO account (login, password, social_id) VALUES (@Login, @Password, @Social)", conn);
//     cmd.Parameters.AddWithValue("@Login", req.Login);
//     cmd.Parameters.AddWithValue("@Password", req.Password);
//     cmd.Parameters.AddWithValue("@Social", req.Social);
//     
//     await cmd.ExecuteNonQueryAsync();
//     return Results.Json(new { ok = true });
// });
//
// app.MapGet("/get-dragon-coins/{id}", async (string id, ILogger<Program> logger) =>
// {
//     logger.LogInformation("get dragon-coins id: {id}", id);
//     await using var conn = new MySqlConnection(connectionString);
//     await conn.OpenAsync();
//     var cmd = new MySqlCommand($"SELECT cash FROM account WHERE login = @id", conn);
//     cmd.Parameters.AddWithValue("@id", id);
//     
//     var result = await cmd.ExecuteScalarAsync();
//     return Results.Json(new { ok = true,  cash = Convert.ToInt32(result) });
// });
//
// app.MapGet("/buyScroll", async (ILogger<Program> logger) =>
// {
//     logger.LogInformation("buy Scroll");
//     await using var conn = new MySqlConnection(connectionStringPlayer);
//     await conn.OpenAsync();
//     var cmd = new MySqlCommand($"INSERT INTO item_award (pid, login, vnum, count, mall)  VALUES (1, 'admin', 25040, 1, 1)", conn);
//     await cmd.ExecuteNonQueryAsync();
//     
//     await using var conn2 = new MySqlConnection(connectionString);
//     await conn2.OpenAsync();
//     var cmd2 = new MySqlCommand("UPDATE account SET cash = cash - 200 WHERE login = 'admin' AND cash >= 200", conn2);
//     await cmd2.ExecuteNonQueryAsync();
//     
//     return Results.Json(new { ok = true });
// });


app.Run();

record req(string message);