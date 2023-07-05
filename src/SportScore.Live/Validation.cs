using SportScore.Live.Exceptions;

namespace SportScore.Live
{
    internal static class Validation
    {
        public static void ValidateInput(string value, string name)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new WebsocketBadInputException($"Input string parameter '{name}' is null or empty. Please correct it.");
        }

        public static void ValidateInput<T>(T value, string name)
        {
            if (Equals(value, default(T)))
                throw new WebsocketBadInputException($"Input parameter '{name}' is null. Please correct it.");
        }

        public static void ValiedateInputCollection<T>(IEnumerable<T> collection, string name)
        {
            ValidateInput(collection, name);

            if (!collection.Any())
                throw new WebsocketBadInputException($"Input collection '{name}' is empty. Please correct it.");
        }

        public static void ValiedateInput(int value, string name, int minvalue = int.MinValue, int maxvalue = int.MaxValue)
        {
            if(value < minvalue)
                throw new WebsocketBadInputException($"Input parameter '{name}' is lower than {minvalue}. Please correct it.");

            if (value > maxvalue)
                throw new WebsocketBadInputException($"Input parameter '{name}' is higher than {maxvalue}. Please correct it.");
        }

        public static void ValidateInput(double value, string name, double minValue = double.MinValue, double maxValue = double.MaxValue)
        {
            if (value < minValue)
            {
                throw new WebsocketBadInputException($"Input parameter '{name}' is lower than {minValue}. Please correct it.");
            }
            if (value > maxValue)
            {
                throw new WebsocketBadInputException($"Input parameter '{name}' is higher than {maxValue}. Please correct it.");
            }
        }
    }
}
