﻿using Redis.OM.Modeling;
using System.Collections;
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
        public int State { get; set; } = -1;
        public bool AudienceAllowed { get; set; } = false;
        public string TurnPassword { get; set; } = "";
        public Dictionary<int, Signaling> SignalingMap { get; set; } = [];
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

    public class Signaling
    {
        public Sdp Lobby { get; set; }
        public Sdp? Player { get; set; }

        [JsonConstructor]
        public Signaling() { }

        public Signaling(Sdp Lobby)
        {
            this.Lobby = Lobby;
            this.Player = null;
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
        public Player Player { get; set; }
        public string LobbyId { get; set; } = "";
        public string Game { get; set; } = "";

        public NewPlayerResponse(Player player, Lobby lobby)
        {
            this.Player = player;
            this.LobbyId = lobby.Id;
            this.Game = lobby.Game;
        }
    }

    public class CheckLobbyResponse
    {
        public string Id { get; set; }
        public string Game { get; set; }
        public int State { get; set; }
        public bool AudienceAllowed { get; set; }

        public CheckLobbyResponse(string Id, string Game, int State, bool Audience)
        {
            this.Id = Id;
            this.Game = Game;
            this.State = State;
            this.AudienceAllowed = Audience;
        }
    }
}
