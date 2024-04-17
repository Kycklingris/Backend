using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Searching;
using System.Reflection.Metadata.Ecma335;

namespace Backend.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly RedisCollection<Backend.Models.Lobby> _lobbies;
        private readonly RedisConnectionProvider _connectionProvider;
        private readonly TimeSpan keyExpire = TimeSpan.FromHours(2);

        public LobbyController(RedisConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
            _lobbies = (RedisCollection<Backend.Models.Lobby>)connectionProvider.RedisCollection<Backend.Models.Lobby>();
        }

        [HttpPost(Name = "CreateLobby")]
        public async Task<Backend.Models.Lobby> NewLobby([FromBody] Backend.Models.CreateLobby newLobby)
        {
            string? Id = null;

            while (Id == null)
            {
                // TODO: Test multiple codes at once, and dynamically add more letters in case of no found codes
                var testId = Backend.UniqueIshString.GenerateString(4);

                if (await _lobbies.FindByIdAsync(testId) == null)
                {
                    Id = testId;
                }
            }

            Backend.Models.Lobby lobby = new(newLobby, Id);

            await _lobbies.InsertAsync(lobby, keyExpire);
            await SetCoturnUser(lobby.Id, lobby.HostUniqueId);

            return lobby;
        }

        [HttpPatch(Name = "LobbyHearbeat")]
        public async Task LobbyHeartbeat([FromBody] Backend.Models.LobbyHeartbeat heartbeatLobby)
        {
            var lobby = await _lobbies.FindByIdAsync(heartbeatLobby.Id);
            if (lobby != null && lobby.HostUniqueId == heartbeatLobby.HostUniqueId)
            {
                await UpdateLobbyTTL(lobby);
            }
        }

        private async Task SetCoturnUser(string username, string password)
        {
            await _connectionProvider.Connection.ExecuteAsync("set", [
                GenerateRedisKeyFromUsername(username),
                GenerateHMacKey(username, password),
                "ex",
                keyExpire.TotalSeconds.ToString()
                ]);
        }

        private async Task UpdateLobbyTTL(Backend.Models.Lobby lobby)
        {
            // Lobby itself
            await _connectionProvider.Connection.ExecuteAsync("expire", [
                "Lobby:" + lobby.Id,
                keyExpire.TotalSeconds.ToString(),
                ]);

            // Host CoTurn Key
            await _connectionProvider.Connection.ExecuteAsync("expire", [
                GenerateRedisKeyFromUsername(lobby.Id),
                keyExpire.TotalSeconds.ToString(),
                ]);

            // Player CoTurn Keys
            foreach (var player in lobby.Players)
            {
                await _connectionProvider.Connection.ExecuteAsync("expire", [
                    GenerateRedisKeyFromUsername(lobby.Id + player.Name),
                    keyExpire.TotalSeconds.ToString(),
                    ]);
            }
        }

        private static string GenerateRedisKeyFromUsername(string username)
        {
            return "turn/realm/" + Backend.Constants.Realm + "/user/" + username + "/key";
        }

        private static string GenerateHMacKey(string username, string password)
        {
            string hashInput = username + ":" + Backend.Constants.Realm + ":" + password;
            return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.ASCII.GetBytes(hashInput)));
        }
    }
}
