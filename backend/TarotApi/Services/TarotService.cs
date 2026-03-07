using System.Security.Cryptography;
using TarotApi.Data;
using TarotApi.Models;

namespace TarotApi.Services;

public class TarotService
{
    private static readonly Dictionary<SpreadType, SpreadPosition[]> SpreadConfigs = new()
    {
        [SpreadType.Single] =
        [
            new(0, "指引", "此刻對你最重要的訊息")
        ],
        [SpreadType.ThreeCardTime] =
        [
            new(0, "過去", "影響當前情況的過去因素"),
            new(1, "現在", "目前的狀態與挑戰"),
            new(2, "未來", "如果沿著目前道路前進的可能發展")
        ],
        [SpreadType.ThreeCardProblem] =
        [
            new(0, "問題", "你目前面對的核心問題"),
            new(1, "原因", "造成這個問題的根本原因"),
            new(2, "對策", "可能的解決方向與建議")
        ],
        [SpreadType.ThreeCardLinear] =
        [
            new(0, "第一張", "牌陣中的第一個訊息"),
            new(1, "第二張", "牌陣中的第二個訊息"),
            new(2, "第三張", "牌陣中的第三個訊息")
        ],
        [SpreadType.CelticCross] =
        [
            new(0, "現狀", "目前的處境與核心問題"),
            new(1, "挑戰", "當前面臨的阻礙或對立力量"),
            new(2, "潛意識", "內心深處的想法與潛在影響"),
            new(3, "過去", "近期影響事件發展的過去因素"),
            new(4, "可能性", "最佳可能結果或目標"),
            new(5, "近未來", "即將發生的事件或影響"),
            new(6, "自我", "你對這個問題的態度與立場"),
            new(7, "環境", "周圍環境與他人的影響"),
            new(8, "希望與恐懼", "內心的期望或擔憂"),
            new(9, "結果", "最終可能的結果")
        ]
    };

    public record DrawnCardResult(TarotCardInfo Card, string Orientation, SpreadPosition Position);

    public List<DrawnCardResult> DrawCards(SpreadType spreadType)
    {
        var positions = SpreadConfigs[spreadType];
        var allCards = TarotCards.All;

        // Fisher-Yates shuffle using cryptographic RNG
        var indices = Enumerable.Range(0, allCards.Count).ToArray();
        for (var i = indices.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var totalCards = spreadType == SpreadType.Single ? positions.Length : positions.Length + 1;
        var results = new List<DrawnCardResult>(totalCards);
        for (var i = 0; i < positions.Length; i++)
        {
            var card = allCards[indices[i]];
            var orientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed";
            results.Add(new DrawnCardResult(card, orientation, positions[i]));
        }

        // Append feeling card for non-Single spreads
        if (spreadType != SpreadType.Single)
        {
            var feelingCard = allCards[indices[positions.Length]];
            var feelingOrientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed";
            var feelingPosition = new SpreadPosition(positions.Length, "你的感受", "你對此問題最真實的內心感受");
            results.Add(new DrawnCardResult(feelingCard, feelingOrientation, feelingPosition));
        }

        return results;
    }

    public static SpreadPosition[] GetPositions(SpreadType spreadType) => SpreadConfigs[spreadType];
}
