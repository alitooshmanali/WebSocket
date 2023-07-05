using SportScore.Live.Enums;

namespace SportScore.Live.Models
{
    public class ReconnectionInfo
    {
        public ReconnectionInfo(ReconnectionType type)
        {
            Type = type;
        }

        public ReconnectionType Type { get; }

        public static ReconnectionInfo Create(ReconnectionType type)
        {
            return new ReconnectionInfo(type);
        }
    }
}
