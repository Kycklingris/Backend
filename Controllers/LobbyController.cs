using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Searching;

namespace Backend.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
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

        [HttpGet(Name = "Check for Lobby")]
        public async Task<IActionResult> CheckLobby([FromBody] Backend.Models.CheckLobby requestedLobby)
        {
            var lobby = await _lobbies.FindByIdAsync(requestedLobby.Id);
            if (lobby != null)
            {
                return CreatedAtAction("CheckLobby", new Backend.Models.CheckLobbyResponse(lobby.Id, lobby.Game));
            }

            return NotFound();
        }

        [HttpPost(Name = "New")]
        public async Task<IActionResult> New([FromBody] Backend.Models.CreateLobby newLobby)
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
            await SetCoturnUser(lobby.Id, lobby.TurnPassword);

            return CreatedAtAction("New", lobby);
        }

        [HttpPatch(Name = "Lobby Set SDP")]
        public async Task<IActionResult> SetSdp([FromBody] Backend.Models.SetLobbySdp lobbySdp)
        {
            var lobby = await _lobbies.FindByIdAsync(lobbySdp.LobbyId);
            if (lobby != null && lobby.TurnPassword == lobbySdp.TurnPassword)
            {
                lobby.Sdp = lobbySdp.Sdp;
                await _lobbies.UpdateAsync(lobby);
                return Ok();
            }

            return BadRequest();
        }

        [HttpGet(Name = "PollPlayers")]
        public async Task<IActionResult> PollPlayers([FromBody] Backend.Models.LobbyHeartbeat requestedLobby)
        {
            var lobby = await _lobbies.FindByIdAsync(requestedLobby.Id);
            if (lobby != null && lobby.TurnPassword == requestedLobby.TurnPassword)
            {
                return CreatedAtAction("PollPlayers", lobby.Players);
            }

            return BadRequest();
        }

        [HttpPatch(Name = "Start Lobby")]
        public async Task<IActionResult> Start([FromBody] Backend.Models.LobbyHeartbeat requestedLobby)
        {
            var lobby = await _lobbies.FindByIdAsync(requestedLobby.Id);
            if (lobby != null && lobby.TurnPassword == requestedLobby.TurnPassword)
            {
                lobby.Started = true;
                await _lobbies.UpdateAsync(lobby);
                return Ok();
            }

            return BadRequest();
        }

        [HttpPatch(Name = "Hearbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] Backend.Models.LobbyHeartbeat heartbeatLobby)
        {
            var lobby = await _lobbies.FindByIdAsync(heartbeatLobby.Id);
            if (lobby != null && lobby.TurnPassword == heartbeatLobby.TurnPassword)
            {
                await UpdateLobbyTTL(lobby, keyExpire.TotalSeconds);
                return Ok();
            }

            return BadRequest();
        }

        [HttpDelete(Name = "Delete")]
        public async Task<IActionResult> Delete([FromBody] Backend.Models.LobbyHeartbeat data)
        {
            var lobby = await _lobbies.FindByIdAsync(data.Id);
            if (lobby != null && lobby.TurnPassword == data.TurnPassword)
            {
                await UpdateLobbyTTL(lobby, 1);
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost(Name = "Join")]
        public async Task<IActionResult> Join([FromBody] Backend.Models.NewPlayer joiningPlayer)
        {
            var lobby = await _lobbies.FindByIdAsync(joiningPlayer.LobbyId.ToUpper());
            if (lobby == null || 
                lobby.Players.Count >= lobby.MaxPlayers ||
                lobby.Sdp.Count == 0 ||
                lobby.Started == true)
            {
                return BadRequest();
            }

            // Check for conflicting player names
            foreach (var lobbyPlayer in lobby.Players)
            {
                if (lobbyPlayer.Name.ToLower() == joiningPlayer.Name.ToLower())
                {
                    return Conflict();
                }
            }
            Backend.Models.Player player = new(joiningPlayer.Name, lobby.Id);

            lobby.Players.Add(player);

            await _lobbies.UpdateAsync(lobby);
            await SetCoturnUser(player.TurnUsername, player.TurnPassword);
            return CreatedAtAction("Join", new Backend.Models.NewPlayerResponse(player, lobby));
        }

        [HttpPatch(Name = "Set Player Sdp")]
        public async Task<IActionResult> SetPlayerSdp([FromBody] Backend.Models.SetSdp playerSdp)
        {
            var lobby = await _lobbies.FindByIdAsync(playerSdp.LobbyId.ToUpper());
            if (lobby != null)
            {
                foreach (var player in lobby.Players)
                {
                    if (player.TurnPassword == playerSdp.TurnPassword)
                    {
                        player.Sdp = playerSdp.Sdp;
                        await _lobbies.UpdateAsync(lobby);
                        return Ok();
                    }
                }
            }

            return BadRequest();
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

        private async Task UpdateLobbyTTL(Backend.Models.Lobby lobby, double seconds)
        {
            // Lobby itself
            await _connectionProvider.Connection.ExecuteAsync("expire", [
                "Lobby:" + lobby.Id,
                seconds.ToString(),
                ]);

            // Host CoTurn Key
            await _connectionProvider.Connection.ExecuteAsync("expire", [
                GenerateRedisKeyFromUsername(lobby.Id),
                seconds.ToString(),
                ]);

            // Player CoTurn Keys
            foreach (var player in lobby.Players)
            {
                await _connectionProvider.Connection.ExecuteAsync("expire", [
                    GenerateRedisKeyFromUsername(player.TurnUsername),
                    seconds.ToString(),
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
