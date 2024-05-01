using Redis.OM.Modeling;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = ["Lobby"])]
    public class Lobby
    {
        [RedisIdField] public string Id { get; set; } = "";
        public int MinPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Game { get; set; } = "";
        public bool Started { get; set; } = false;
        public string TurnPassword { get; set; } = "";
        public List<Sdp> Sdp { get; set; } = [];
        public List<Player> Players { get; set; } = [];

        [JsonConstructor]
        public Lobby() {}

        public Lobby(int minPlayers, int maxPlayers, string game, string Id)
        {
            this.Id = Id;
            this.MinPlayers = minPlayers;
            this.MaxPlayers = maxPlayers;
            this.Game = game;
            this.TurnPassword = Backend.UniqueIshString.GenerateStringLowercase(20);
            this.Players = [];
        }
    }

    public class Sdp
    {
        public string Session { get; set; } = "";
        public List<IceCandidate> IceCandidates { get; set; } = [];
    }

    public class IceCandidate
    {
        public string Media { get; set; } = "";
        public int Index { get; set; } = 0;
        public string Name { get; set; } = "";
    }


    public class Player
    {
        public string Name { get; set; } = "";
        public string TurnUsername { get; set; } = "";
        public string TurnPassword { get; set; } = "";
        public Sdp? Sdp { get; set; }

        [JsonConstructor]
        public Player() { }

        public Player(string Name, string lobbyId)
        {
            this.Name = Name;
            this.TurnUsername = lobbyId + "." + Name;
            this.TurnPassword = Backend.UniqueIshString.GenerateStringLowercase(20);
        }
    }

    public class NewPlayerResponse
    {
        public Player player { get; set; }
        public string LobbyId { get; set; } = "";
        public string Game { get; set; } = "";
        public Sdp LobbySdp { get; set; }

        public NewPlayerResponse(Player player, Lobby lobby)
        {
            this.player = player;
            this.LobbyId = lobby.Id;
            this.Game = lobby.Game;

            for (int i = 0; i < lobby.Players.Count; i++)
            {
                if (lobby.Players[i].TurnPassword == player.TurnPassword)
                {
                    this.LobbySdp = lobby.Sdp[i];
                }
            }
        }
    }

    public class CheckLobbyResponse
    {
        public string Id { get; set; }
        public string Game { get; set; }

        public CheckLobbyResponse(string Id, string Game)
        {
            this.Id = Id;
            this.Game = Game;
        }
    }
}
