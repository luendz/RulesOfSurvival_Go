namespace ROS.Game.Core
{
    public interface IGameDataDefinition
    {
        string StableId { get; }
        DataConfidence Confidence { get; }
    }

    public static class GameDataId
    {
        public const int MaxLength = 64;

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];

                bool isLowercaseLetter =
                    character >= 'a' && character <= 'z';

                bool isDigit =
                    character >= '0' && character <= '9';

                bool isSeparator =
                    character == '_' ||
                    character == '-' ||
                    character == '.';

                if (!isLowercaseLetter && !isDigit && !isSeparator)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
