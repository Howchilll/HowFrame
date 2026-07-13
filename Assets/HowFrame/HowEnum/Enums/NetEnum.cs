namespace HowEnum
{
    public static class NetEnum
    {
        public class Tag { }

        public static readonly EnumKey<Tag> Player = new("Player");
        public static readonly EnumKey<Tag> Player1 = new("Player1");
        public static readonly EnumKey<Tag> Player2 = new("Player2");
        public static readonly EnumKey<Tag> Player3 = new("Player3");
        public static readonly EnumKey<Tag> LobbyRoomState = new("LobbyRoomState");
        public static readonly EnumKey<Tag> GameStateData = new("GameStateData");
        
        public static EnumKey<Tag> Convert(string value)
        {
            switch (value)
            {
                case "Player": return Player;
                case "Player1": return Player1;
                case "Player2": return Player2;
                case "Player3": return Player3;
                case "LobbyRoomState": return LobbyRoomState;
                case "GameStateData": return GameStateData;
                default: throw new System.ArgumentException($"Unknown value: {value}");
            }
        }

        public static System.Collections.Generic.List<EnumKey<Tag>> GetAll()
        {
            return new System.Collections.Generic.List<EnumKey<Tag>>
            {
                Player,
                Player1,
                Player2,
                Player3,
                LobbyRoomState,
                GameStateData,
            };
        }
    }
}
