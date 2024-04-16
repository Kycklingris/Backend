using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Searching;

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
            var rand = new Random();
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

        private async Task SetCoturnUser(string userName, string password)
        {
            var hashInput = userName + ":smorsoft.com:" + password;
            await _connectionProvider.Connection.ExecuteAsync("set", [
                "turn/realm/smorsoft.com/user/" + userName + "/key",
                Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.ASCII.GetBytes(hashInput))),
                "ex",
                keyExpire.TotalSeconds.ToString()
                ]);
        }
    }
}
