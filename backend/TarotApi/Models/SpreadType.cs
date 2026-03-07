using System.Text.Json.Serialization;

namespace TarotApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpreadType
{
    Single,
    ThreeCardTime,
    ThreeCardProblem,
    ThreeCardLinear,
    CelticCross
}
