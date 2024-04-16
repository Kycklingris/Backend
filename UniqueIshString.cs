using Microsoft.AspNetCore.Routing.Constraints;
using System.Reflection.Metadata.Ecma335;

namespace Backend
{
    public class UniqueIshString
    {
        static readonly string[] LobbyIdTokens = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];

        public static string GenerateString(int length)
        {
            var output = "";

            for (int i = 0; i < length; i++)
            {
                output += LobbyIdTokens[Random.Shared.Next(0, LobbyIdTokens.Length)];
            }

            return output;
        }

        public static string GenerateStringLowercase(int length)
        {
            var output = GenerateString(length);

            return output.ToLower();
        }
    }
}
