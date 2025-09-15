using metin2freebsdapi.Helpers;
using metin2freebsdapi.Models;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;

namespace metin2freebsdapi.Endpoints;

internal static class WebsiteEndpoints
{
    internal static void AddWebsiteEndpoints(this WebApplication app)
    {
        app.MapPost("/account-register", async (HttpContext httpContext, RegistrationCredentials credentials) =>
        {
            if (ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            await using var accountDb = new MySqlConnection(EnvironmentVariables.SqlAccount);
            await accountDb.OpenAsync();

            try
            {
                await using var checkExisting = new MySqlCommand("""
SELECT
    COUNT(CASE WHEN login = @login THEN 1 END) AS loginCount,
    COUNT(CASE WHEN email = @email THEN 1 END) AS emailCount
FROM account
""", accountDb);
                checkExisting.Parameters.AddWithValue("@login", credentials.Username);
                checkExisting.Parameters.AddWithValue("@email", credentials.Email);

                await using var reader = await checkExisting.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var loginCount = reader.GetInt32("loginCount");
                    var emailCount = reader.GetInt32("emailCount");

                    if (loginCount > 0) throw new Exception("User already exists.");
                    if (emailCount > 0) throw new Exception("This email is taken.");
                }

                await reader.CloseAsync();

                await using var createNew =
                    new MySqlCommand(
                        "INSERT INTO account (login, password, email, social_id) VALUES (@login, @password, @email, @social_id)",
                        accountDb);
                createNew.Parameters.AddWithValue("@login", credentials.Username);
                createNew.Parameters.AddWithValue("@password", credentials.Password);
                createNew.Parameters.AddWithValue("@email", credentials.Email);
                createNew.Parameters.AddWithValue("@social_id", credentials.Security);

                await createNew.ExecuteNonQueryAsync();
                
                return Results.Json(new {ok = true, message = "Registration successful."});
            }
            catch (Exception ex)
            {
                return Results.Json(new {ok = false, message = ex.Message});
            }
        });

        app.MapPost("/account-login", async (HttpContext httpContext, LoginCredentials credentials) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            await using var accountDb = new MySqlConnection(EnvironmentVariables.SqlAccount);
            await accountDb.OpenAsync();

            try
            {
                await using var checkExisting =
                    new MySqlCommand("SELECT * FROM account WHERE login = @login", accountDb);
                checkExisting.Parameters.AddWithValue("@login", credentials.Username);
                
                await using var reader = await checkExisting.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var login = reader.GetString("login");
                    var password = reader.GetString("password");
                    var id = reader.GetInt32("id");
                    var coins = reader.GetInt32("cash");
                    
                    if (password != credentials.Password) throw new Exception("Wrong password.");

                    return Results.Json(new
                        { ok = true, data = new User(UserId: id, Username: login, ShopCoins: coins) });
                }
                
                throw new Exception("Account doesn't exist.");
            }
            catch (Exception ex)
            {
                return Results.Json( new { ok = false, message = ex.Message});
            }
        });

        app.MapPost("/award-item", async (HttpContext httpContext, AwardItem awardItem) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            await using var playerDb = new MySqlConnection(EnvironmentVariables.SqlPlayer);
            await playerDb.OpenAsync();
            
            await using var accountDb = new MySqlConnection(EnvironmentVariables.SqlAccount);
            await accountDb.OpenAsync();

            try
            {
                var checkCoins = new MySqlCommand("SELECT * FROM coins WHERE id = @id", accountDb);
                await using var reader = await checkCoins.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    if (reader.GetInt32("cash") < awardItem.Price) throw new Exception("Not enough coins.");
                }
                
                var deliverItem = new MySqlCommand("INSERT INTO item_award (login, vnum, count, mall, why) VALUES (@login, @vnum, @count, @mall, @why", playerDb);
                deliverItem.Parameters.AddWithValue("@login", awardItem.Username);
                deliverItem.Parameters.AddWithValue("@vnum", awardItem.Vnum);
                deliverItem.Parameters.AddWithValue("@count", awardItem.Quantity);
                deliverItem.Parameters.AddWithValue("@mall", 1);
                deliverItem.Parameters.AddWithValue("@why", "item shop purchase");
                
                await deliverItem.ExecuteNonQueryAsync();
                
                var subtractCoins = new MySqlCommand("UPDATE account SET cash = cash - @price WHERE login = @login", accountDb);
                subtractCoins.Parameters.AddWithValue("@login", awardItem.Username);
                subtractCoins.Parameters.AddWithValue("@price", awardItem.Price);
                
                await subtractCoins.ExecuteNonQueryAsync();
                
                return Results.Json( new { ok = true, message = "Purchase successful."});
            }
            catch (Exception ex)
            {
                return Results.Json(new {ok = false, message = ex.Message});
            }
        });

        app.MapPost("/add-coins", async (HttpContext httpContext, CoinTopUp coinTopUp) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            await using var accountDb = new MySqlConnection(EnvironmentVariables.SqlAccount);
            await accountDb.OpenAsync();

            try
            {
                var addCoins = new MySqlCommand("UPDATE account SET cash = cash + @amount WHERE login = @login", accountDb);
                addCoins.Parameters.AddWithValue("@login", coinTopUp.Username);
                addCoins.Parameters.AddWithValue("@amount", coinTopUp.Amount);
                
                await addCoins.ExecuteNonQueryAsync();
                
                return Results.Json(new { ok = true, message = "Coins added successfully." });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, message = ex.Message});
            }
        });

        app.MapGet("/server-status", async (HttpContext httpContext, IMemoryCache cache) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });

            const string cacheKey = "server_status";

            if (cache.TryGetValue(cacheKey, out ServerStatus? cachedServerStatus))
            {
                return Results.Json(new
                    { ok = true, data = cachedServerStatus, message = "Cached server status result." });
            }
            
            try
            {
                await using var accountDb = new MySqlConnection(EnvironmentVariables.SqlAccount);
                await accountDb.OpenAsync();
                
                var getRegisteredAccounts = new MySqlCommand("SELECT COUNT(*) FROM account", accountDb);
                
                var getRegisteredAccountsResult = await getRegisteredAccounts.ExecuteScalarAsync();
                
                var registeredAccounts = Convert.ToInt32(getRegisteredAccountsResult);
                
                var onlineStatusResponse =
                    await ServerHelpers.SendCommandAsync([EnvironmentVariables.AdminPagePassword, "IS_SERVER_UP"]);

                var onlineStatusTokens = onlineStatusResponse.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
                
                var last = new string(onlineStatusTokens.Last().Where(char.IsLetter).ToArray());
                var onlineStatus = string.Equals(last, "YES", StringComparison.OrdinalIgnoreCase);

                if (!onlineStatus)
                {
                    var serverStatusOffline = new ServerStatus(onlineStatus, 0, 0, 0, 0, registeredAccounts);

                    cache.Set(cacheKey, serverStatusOffline, TimeSpan.FromMinutes(10));
                    
                    return Results.Json(new
                        { ok = true, data = serverStatusOffline, message = "Server is offline." });
                }
                
                var usersOnlineResponse = await ServerHelpers.SendCommandAsync([EnvironmentVariables.AdminPagePassword, "USER_COUNT"]);
                var usersOnlineTokens = usersOnlineResponse.Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries);
                
                var usersOnline = usersOnlineTokens.SkipWhile(t => !int.TryParse(t, out _)).ToArray();

                var serverStatusOnline = new ServerStatus(onlineStatus, int.Parse(usersOnline[0]), int.Parse(usersOnline[1]),
                    int.Parse(usersOnline[2]), int.Parse(usersOnline[3]), registeredAccounts);
                
                cache.Set(cacheKey, serverStatusOnline, TimeSpan.FromMinutes(10));

                return Results.Json(new { ok = true, data = serverStatusOnline, message = "Server is online." });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, message = ex.Message });
            }
        });

        app.MapGet("/player-rankings", async (HttpContext httpContext, IMemoryCache cache) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            const string cacheKey = "player_rankings";
            
            if (cache.TryGetValue(cacheKey, out List<PlayerRank>? cachedPlayerRankings))
            {
                return Results.Json(new
                    { ok = true, data = cachedPlayerRankings, message = "Cached player rankings." });
            }
            
            var playerDb = new MySqlConnection(EnvironmentVariables.SqlPlayer);
            await playerDb.OpenAsync();
            
            var rankings = new List<PlayerRank>();

            try
            {
                var getRankings =
                    new MySqlCommand(
                        "SELECT p.account_id, p.name, p.job, p.level, p.playtime, p.exp, pi.empire FROM player p LEFT JOIN player_index pi ON pi.id = p.account_id WHERE p.name NOT LIKE '[%]%' ORDER BY p.level DESC, p.exp DESC, p.playtime DESC", playerDb);
                await using var rankingsReader = await getRankings.ExecuteReaderAsync();

                while (await rankingsReader.ReadAsync())
                {
                    rankings.Add(new PlayerRank(
                        rankingsReader.GetString("name"),
                        rankingsReader.GetInt32("job"),
                        rankingsReader.GetInt32("level"),
                        rankingsReader.GetInt32("playtime"),
                        rankingsReader.GetInt32("exp"),
                        rankingsReader.GetInt32("empire")
                        ));
                }
                
                cache.Set(cacheKey, rankings, TimeSpan.FromMinutes(10));

                return Results.Json(new { ok = true, data = rankings, message = "Player rankings." });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, message = ex.Message });
            }
        });

        app.MapGet("/guild-rankings", async (HttpContext httpContext, IMemoryCache cache) =>
        {
            if (!ValidateSecrets.ValidateWebSecret(httpContext)) return Results.Json(new { ok = false, message = "Unauthorized request." });
            
            const string cacheKey = "guild_rankings";
            
            if (cache.TryGetValue(cacheKey, out List<GuildRank>? cachedGuildRankings))
            {
                return Results.Json(new
                    { ok = true, data = cachedGuildRankings, message = "Cached guild rankings." });
            }
            
            var playerDb = new MySqlConnection(EnvironmentVariables.SqlPlayer);
            await playerDb.OpenAsync();
            
            var rankings =  new List<GuildRank>();

            try
            {
                var getRankings = new MySqlCommand(
                    "SELECT g.name AS guild_name, g.level, g.exp, g.win, g.draw, g.loss, g.master, p.name as master_name, pi.empire FROM guild g LEFT JOIN player p ON p.id = g.master LEFT JOIN player_index pi ON pi.id = p.account_id ORDER BY g.level DESC, g.win DESC, g.draw DESC, g.exp DESC", playerDb);
                await using var rankingsReader = await getRankings.ExecuteReaderAsync();

                while (await rankingsReader.ReadAsync())
                {
                    rankings.Add(new GuildRank(
                        rankingsReader.GetString("guild_name"),
                        rankingsReader.GetString("master_name"),
                        rankingsReader.GetInt32("empire"),
                        rankingsReader.GetInt32("level"),
                        rankingsReader.GetInt32("exp"),
                        rankingsReader.GetInt32("win"),
                        rankingsReader.GetInt32("draw"),
                        rankingsReader.GetInt32("loss")
                        ));
                }
                
                cache.Set(cacheKey, rankings, TimeSpan.FromMinutes(10));
                
                return Results.Json(new { ok = true, data = rankings, message = "Guild rankings." });
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, message = ex.Message });
            }
        });
    }
}