using System;
using System.Globalization;
using BreakInfinity;
using Newtonsoft.Json;

namespace Crumble.Core
{
    /// <summary>
    /// Serializes BigDouble as a "&lt;mantissa&gt;e&lt;exponent&gt;" string (e.g. "1.5e300").
    /// A raw JSON number would silently clip anything beyond double range (~1.8e308).
    /// The mantissa uses round-trip ("R") formatting so no precision is lost; it is always
    /// normalized to [1, 10) so it can never itself contain scientific notation.
    /// </summary>
    public sealed class BigDoubleJsonConverter : JsonConverter<BigDouble>
    {
        public override void WriteJson(JsonWriter writer, BigDouble value, JsonSerializer serializer)
        {
            writer.WriteValue(
                value.Mantissa.ToString("R", CultureInfo.InvariantCulture)
                + "e"
                + value.Exponent.ToString(CultureInfo.InvariantCulture));
        }

        public override BigDouble ReadJson(JsonReader reader, Type objectType, BigDouble existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                {
                    var text = (string)reader.Value;
                    var split = text.LastIndexOf('e');
                    if (split < 0)
                    {
                        return new BigDouble(double.Parse(text, CultureInfo.InvariantCulture));
                    }

                    var mantissa = double.Parse(text.Substring(0, split), CultureInfo.InvariantCulture);
                    var exponent = long.Parse(text.Substring(split + 1), CultureInfo.InvariantCulture);
                    return new BigDouble(mantissa, exponent);
                }
                case JsonToken.Integer:
                case JsonToken.Float:
                    // Tolerate plain numbers (hand-edited or legacy saves).
                    return new BigDouble(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));
                case JsonToken.Null:
                    return BigDouble.Zero;
                default:
                    throw new JsonSerializationException(
                        $"Cannot convert JSON token '{reader.TokenType}' to BigDouble.");
            }
        }
    }
}
