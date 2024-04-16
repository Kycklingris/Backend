using Redis.OM.Modeling;

namespace Backend.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = ["Lobby"])]
    public class Lobby
    {
        [RedisIdField] public string Id { get; set; }
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Game {  get; set; }
        public string HostUniqueId { get; set; }
        public Player[] Players { get; set; } = [];

        public Lobby(CreateLobby createLobby, string Id)
        {
            this.Id = Id;
            this.MinPlayers = createLobby.MinPlayers;
            this.MaxPlayers = createLobby.MaxPlayers;
            this.Game = createLobby.Game;
            this.HostUniqueId = Backend.UniqueIshString.GenerateStringLowercase(5);
        }
    }

    public class Player
    {
        public string? Name { get; set; }
        public bool Admin = false;
        public string? UniqueId {  get; set; }
    }

    public class CreateLobby
    {
        public string Game { get; set; }
        public int MaxPlayers { get; set; }
        public int MinPlayers { get; set; }
    }
}
