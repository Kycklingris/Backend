using Redis.OM.Modeling;
using System.Text.Json.Serialization;

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

        [JsonConstructor]
        public Lobby() {}

        public Lobby(CreateLobby createLobby, string Id)
        {
            this.Id = Id;
            this.MinPlayers = createLobby.MinPlayers;
            this.MaxPlayers = createLobby.MaxPlayers;
            this.Game = createLobby.Game;
            this.HostUniqueId = Backend.UniqueIshString.GenerateStringLowercase(20);
            this.Players = [];
        }
    }

    public class LobbyHeartbeat
    {
        public string Id { get; set; }
        public string HostUniqueId { get; set; }

    }

    public class Player
    {
        public string Name { get; set; } = "";
        public bool Admin = false;
        public string UniqueId { get; set; } = "";

        [JsonConstructor]
        public Player() { }
    }

    public class CreateLobby
    {
        public string Game { get; set; } = "undefined";
        public int MaxPlayers { get; set; }
        public int MinPlayers { get; set; }
    }
}
