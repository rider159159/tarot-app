namespace TarotApi.Data;

public record TarotCardInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NameCht { get; init; } = string.Empty;
    public string Arcana { get; init; } = string.Empty; // "major" | "minor"
    public string? Suit { get; init; } // "wands"|"cups"|"swords"|"pentacles"
    public int Number { get; init; }
    public string MeaningUpright { get; init; } = string.Empty;
    public string MeaningReversed { get; init; } = string.Empty;
    public string[] Keywords { get; init; } = [];
}

public static class TarotCards
{
    private static readonly Dictionary<string, TarotCardInfo> _cardsById;

    public static IReadOnlyList<TarotCardInfo> All { get; }

    static TarotCards()
    {
        var cards = new List<TarotCardInfo>(78);
        cards.AddRange(MajorArcana);
        cards.AddRange(MinorArcana);
        All = cards.AsReadOnly();
        _cardsById = cards.ToDictionary(c => c.Id);
    }

    public static TarotCardInfo? GetById(string id) =>
        _cardsById.GetValueOrDefault(id);

    // ===== Major Arcana (22 cards) =====

    private static readonly TarotCardInfo[] MajorArcana =
    [
        new() { Id = "major_00_fool", Name = "The Fool", NameCht = "愚者", Arcana = "major", Number = 0, MeaningUpright = "新的開始、冒險、自由、天真無邪、無限可能。代表一段旅程的起點，充滿信心地踏入未知，擁抱生命中的新機會。", MeaningReversed = "魯莽、冒失、不計後果、猶豫不決、錯失良機。暗示缺乏規劃或過於天真，需要更謹慎地評估風險。", Keywords = ["新開始", "冒險", "自由", "天真", "可能性"] },
        new() { Id = "major_01_magician", Name = "The Magician", NameCht = "魔術師", Arcana = "major", Number = 1, MeaningUpright = "創造力、意志力、自信、技能、資源充足。你擁有實現目標所需的一切工具，現在是將想法付諸行動的時候。", MeaningReversed = "操縱、欺騙、能力未發揮、缺乏方向。才能被浪費或被用於不正當的目的，需要重新審視自己的意圖。", Keywords = ["創造力", "意志力", "自信", "技能", "行動"] },
        new() { Id = "major_02_high_priestess", Name = "The High Priestess", NameCht = "女祭司", Arcana = "major", Number = 2, MeaningUpright = "直覺、潛意識、內在智慧、神秘、靜默。傾聽內心的聲音，答案隱藏在表象之下，耐心等待真相浮現。", MeaningReversed = "忽視直覺、表面化、秘密被揭露、困惑。過度依賴理性而忽略內在感受，或有隱藏的資訊尚未被發現。", Keywords = ["直覺", "潛意識", "智慧", "神秘", "耐心"] },
        new() { Id = "major_03_empress", Name = "The Empress", NameCht = "皇后", Arcana = "major", Number = 3, MeaningUpright = "豐饒、母性、自然、滋養、感官享受。代表創造力的結實與豐收，生活中充滿美好與富足，適合培育新事物。", MeaningReversed = "過度依賴、創造力受阻、忽視自我照顧、空虛。可能過度付出而忘記照顧自己，或感到生活缺乏滋養。", Keywords = ["豐饒", "母性", "自然", "滋養", "創造"] },
        new() { Id = "major_04_emperor", Name = "The Emperor", NameCht = "皇帝", Arcana = "major", Number = 4, MeaningUpright = "權威、結構、穩定、領導力、紀律。透過秩序和規劃來建立穩固的基礎，展現負責任的領導風範。", MeaningReversed = "專制、僵化、控制慾過強、缺乏彈性。權力被濫用或過度追求控制，需要學習放手與信任他人。", Keywords = ["權威", "結構", "穩定", "領導", "紀律"] },
        new() { Id = "major_05_hierophant", Name = "The Hierophant", NameCht = "教皇", Arcana = "major", Number = 5, MeaningUpright = "傳統、信仰、教導、體制、精神指引。尋求智慧導師的建議，遵循既有的規範與傳統可以帶來穩定。", MeaningReversed = "打破常規、非傳統、質疑權威、個人信念。挑戰既有體制或選擇走自己的路，不再盲目服從。", Keywords = ["傳統", "信仰", "教導", "體制", "指引"] },
        new() { Id = "major_06_lovers", Name = "The Lovers", NameCht = "戀人", Arcana = "major", Number = 6, MeaningUpright = "愛情、和諧、關係、價值觀的選擇、結合。代表深層的情感連結與重要的人生抉擇，需要跟隨內心做出決定。", MeaningReversed = "不和諧、價值觀衝突、錯誤的選擇、關係失衡。面臨困難的抉擇或關係中出現裂痕，需要重新審視。", Keywords = ["愛情", "和諧", "選擇", "關係", "結合"] },
        new() { Id = "major_07_chariot", Name = "The Chariot", NameCht = "戰車", Arcana = "major", Number = 7, MeaningUpright = "勝利、決心、意志力、前進、克服障礙。憑藉堅定的信念和自律精神，戰勝困難並取得成功。", MeaningReversed = "失去控制、方向不明、挫敗、缺乏動力。內在衝突導致無法前進，或過度激進而偏離正軌。", Keywords = ["勝利", "決心", "意志力", "前進", "克服"] },
        new() { Id = "major_08_strength", Name = "Strength", NameCht = "力量", Arcana = "major", Number = 8, MeaningUpright = "內在力量、勇氣、耐心、柔軟的堅強、自我控制。以溫柔而堅定的方式面對挑戰，真正的力量來自內心。", MeaningReversed = "自我懷疑、軟弱、缺乏信心、內在恐懼。面對困難時失去勇氣，或試圖用強硬手段掩飾內心的不安。", Keywords = ["內在力量", "勇氣", "耐心", "堅強", "自制"] },
        new() { Id = "major_09_hermit", Name = "The Hermit", NameCht = "隱者", Arcana = "major", Number = 9, MeaningUpright = "內省、獨處、尋求真理、智慧、引路人。退一步審視人生，在孤獨中找到內在的光明與方向。", MeaningReversed = "孤立、逃避、拒絕幫助、過度封閉。過度退縮而與世界脫節，或害怕面對現實而選擇逃避。", Keywords = ["內省", "獨處", "真理", "智慧", "沉思"] },
        new() { Id = "major_10_wheel_of_fortune", Name = "Wheel of Fortune", NameCht = "命運之輪", Arcana = "major", Number = 10, MeaningUpright = "命運轉折、好運、循環、機會、變化。生命的巨輪正在轉動，即將迎來正面的轉變與新的機遇。", MeaningReversed = "厄運、抗拒改變、失控、不順遂的循環。命運似乎不站在你這邊，但這只是暫時的低谷，需要適應變化。", Keywords = ["命運", "轉折", "好運", "循環", "變化"] },
        new() { Id = "major_11_justice", Name = "Justice", NameCht = "正義", Arcana = "major", Number = 11, MeaningUpright = "公正、真相、因果、平衡、法律。行為的後果正在顯現，公正的判決即將到來，誠實面對一切。", MeaningReversed = "不公正、逃避責任、偏見、失衡。真相被掩蓋或判斷有失偏頗，需要重新檢視公平性。", Keywords = ["公正", "真相", "因果", "平衡", "誠實"] },
        new() { Id = "major_12_hanged_man", Name = "The Hanged Man", NameCht = "倒吊人", Arcana = "major", Number = 12, MeaningUpright = "暫停、犧牲、新視角、放手、等待。從不同的角度看待事物，有時候停下腳步反而能看見新的可能。", MeaningReversed = "拖延、無謂的犧牲、抗拒改變、固執。不願意放下舊有的觀念或習慣，導致停滯不前。", Keywords = ["暫停", "犧牲", "新視角", "放手", "等待"] },
        new() { Id = "major_13_death", Name = "Death", NameCht = "死神", Arcana = "major", Number = 13, MeaningUpright = "結束、轉變、蛻變、放下過去、新生。舊的事物正在結束，為新的開始騰出空間，這是必要的蛻變過程。", MeaningReversed = "抗拒改變、恐懼結束、停滯、無法放手。緊抓著不再適合的事物，阻礙了自然的轉變與成長。", Keywords = ["結束", "轉變", "蛻變", "放下", "新生"] },
        new() { Id = "major_14_temperance", Name = "Temperance", NameCht = "節制", Arcana = "major", Number = 14, MeaningUpright = "平衡、調和、耐心、中庸之道、療癒。在極端之間找到平衡點，以穩定和諧的態度處理事務。", MeaningReversed = "失衡、過度、缺乏耐心、衝突。生活中某些方面過度傾斜，需要重新找回和諧與節制。", Keywords = ["平衡", "調和", "耐心", "中庸", "療癒"] },
        new() { Id = "major_15_devil", Name = "The Devil", NameCht = "惡魔", Arcana = "major", Number = 15, MeaningUpright = "束縛、誘惑、物質主義、陰暗面、執著。被慾望或恐懼所束縛，需要正視自己的陰暗面才能獲得解脫。", MeaningReversed = "解脫、覺醒、掙脫束縛、面對陰影。開始意識到束縛自己的是什麼，準備好擺脫不健康的模式。", Keywords = ["束縛", "誘惑", "物質", "陰暗面", "執著"] },
        new() { Id = "major_16_tower", Name = "The Tower", NameCht = "塔", Arcana = "major", Number = 16, MeaningUpright = "突變、崩塌、覺醒、解放、真相揭露。突如其來的變故摧毀了虛假的結構，雖然痛苦但帶來必要的覺醒。", MeaningReversed = "逃避災難、恐懼改變、延遲崩塌、內在動盪。試圖阻止不可避免的改變，或在內心經歷無聲的瓦解。", Keywords = ["突變", "崩塌", "覺醒", "解放", "真相"] },
        new() { Id = "major_17_star", Name = "The Star", NameCht = "星星", Arcana = "major", Number = 17, MeaningUpright = "希望、靈感、平靜、信心、心靈更新。經歷風暴後的寧靜，重新找到希望與方向，未來一片光明。", MeaningReversed = "失去希望、悲觀、失去信心、斷開連結。暫時看不到前方的光明，需要重新與內在的希望連結。", Keywords = ["希望", "靈感", "平靜", "信心", "更新"] },
        new() { Id = "major_18_moon", Name = "The Moon", NameCht = "月亮", Arcana = "major", Number = 18, MeaningUpright = "幻覺、恐懼、潛意識、不確定、夢境。事情並非表面所見，需要穿越迷霧與恐懼，相信直覺的引導。", MeaningReversed = "真相浮現、克服恐懼、釋放焦慮、清明。迷霧逐漸散去，之前模糊不清的事情開始變得明朗。", Keywords = ["幻覺", "恐懼", "潛意識", "不確定", "直覺"] },
        new() { Id = "major_19_sun", Name = "The Sun", NameCht = "太陽", Arcana = "major", Number = 19, MeaningUpright = "快樂、成功、活力、光明、正面能量。一切都在陽光下清晰可見，充滿歡樂與成就感的美好時期。", MeaningReversed = "暫時的陰霾、過度樂觀、延遲的成功、內在不安。快樂暫時被遮蔽，但陽光終究會再次照耀。", Keywords = ["快樂", "成功", "活力", "光明", "正面"] },
        new() { Id = "major_20_judgement", Name = "Judgement", NameCht = "審判", Arcana = "major", Number = 20, MeaningUpright = "覺醒、重生、反思、召喚、人生回顧。深刻的自我反省帶來靈性的覺醒，準備好迎接生命的新階段。", MeaningReversed = "自我否定、拒絕反省、錯過機會、內心批判。不願面對過去的行為或逃避必要的自我審視。", Keywords = ["覺醒", "重生", "反思", "召喚", "審視"] },
        new() { Id = "major_21_world", Name = "The World", NameCht = "世界", Arcana = "major", Number = 21, MeaningUpright = "完成、圓滿、成就、整合、旅程終點。一個重要的人生階段圓滿完成，獲得了深刻的理解與滿足感。", MeaningReversed = "未完成、缺乏結束感、延遲圓滿、尋求閉合。尚有未完成的課題需要處理，才能真正進入下一個階段。", Keywords = ["完成", "圓滿", "成就", "整合", "滿足"] },
    ];

    // ===== Minor Arcana - Wands (14 cards) =====

    private static readonly TarotCardInfo[] WandsCards =
    [
        new() { Id = "minor_wands_01_ace", Name = "Ace of Wands", NameCht = "權杖一", Arcana = "minor", Suit = "wands", Number = 1, MeaningUpright = "靈感、新機會、創造力、熱情、動力。一股全新的創造能量正在萌芽，適合開展新計畫或追求新目標。", MeaningReversed = "延遲、缺乏方向、創意受阻、虛假的開始。能量被壓抑或計畫尚未成熟，需要耐心等待適當時機。", Keywords = ["靈感", "新機會", "創造力", "熱情", "開始"] },
        new() { Id = "minor_wands_02", Name = "Two of Wands", NameCht = "權杖二", Arcana = "minor", Suit = "wands", Number = 2, MeaningUpright = "規劃、決策、展望未來、野心、掌控。站在十字路口做出選擇，為未來的冒險做好充分準備。", MeaningReversed = "猶豫不決、害怕未知、缺乏計畫、視野狹隘。過度安於現狀而錯失擴展的機會。", Keywords = ["規劃", "決策", "展望", "野心", "選擇"] },
        new() { Id = "minor_wands_03", Name = "Three of Wands", NameCht = "權杖三", Arcana = "minor", Suit = "wands", Number = 3, MeaningUpright = "擴展、遠見、進步、機會來臨、領導。早期的努力開始見到成效，視野開闊，更大的機會正在靠近。", MeaningReversed = "延遲回報、缺乏遠見、障礙、計畫受阻。預期的結果尚未出現，需要調整策略或保持耐心。", Keywords = ["擴展", "遠見", "進步", "機會", "領導"] },
        new() { Id = "minor_wands_04", Name = "Four of Wands", NameCht = "權杖四", Arcana = "minor", Suit = "wands", Number = 4, MeaningUpright = "慶祝、和諧、家庭、里程碑、穩定基礎。值得慶祝的時刻到來，享受努力帶來的成果與歸屬感。", MeaningReversed = "缺乏和諧、不穩定、過渡期、慶祝受阻。預期的喜悅被延遲，或家庭與團體中出現不和諧。", Keywords = ["慶祝", "和諧", "家庭", "里程碑", "穩定"] },
        new() { Id = "minor_wands_05", Name = "Five of Wands", NameCht = "權杖五", Arcana = "minor", Suit = "wands", Number = 5, MeaningUpright = "競爭、衝突、挑戰、多方角力、成長。面對競爭與意見分歧，但這些摩擦能激發出更好的結果。", MeaningReversed = "避免衝突、內在掙扎、達成共識、緊張緩和。衝突正在消退，或選擇退讓以避免不必要的爭執。", Keywords = ["競爭", "衝突", "挑戰", "角力", "成長"] },
        new() { Id = "minor_wands_06", Name = "Six of Wands", NameCht = "權杖六", Arcana = "minor", Suit = "wands", Number = 6, MeaningUpright = "勝利、榮耀、認可、自信、公開成功。你的努力獲得了他人的肯定與讚賞，享受這份應得的榮光。", MeaningReversed = "失敗、缺乏認可、自我懷疑、虛名。期待的掌聲沒有到來，或成功帶來的是壓力而非喜悅。", Keywords = ["勝利", "榮耀", "認可", "自信", "成功"] },
        new() { Id = "minor_wands_07", Name = "Seven of Wands", NameCht = "權杖七", Arcana = "minor", Suit = "wands", Number = 7, MeaningUpright = "堅守立場、防禦、勇氣、堅持、挑戰。面對來自四面八方的挑戰，堅定地捍衛自己的信念與地位。", MeaningReversed = "放棄、不堪重負、退讓、失去立場。壓力過大而考慮妥協，或感到無力繼續戰鬥。", Keywords = ["堅守", "防禦", "勇氣", "堅持", "捍衛"] },
        new() { Id = "minor_wands_08", Name = "Eight of Wands", NameCht = "權杖八", Arcana = "minor", Suit = "wands", Number = 8, MeaningUpright = "迅速行動、進展、消息到來、旅行、勢不可擋。事情以極快的速度推進，把握這股強大的前進動力。", MeaningReversed = "延遲、阻礙、匆忙決定、等待、方向混亂。原本快速的進展突然減速，需要耐心等待正確的時機。", Keywords = ["迅速", "進展", "消息", "旅行", "動力"] },
        new() { Id = "minor_wands_09", Name = "Nine of Wands", NameCht = "權杖九", Arcana = "minor", Suit = "wands", Number = 9, MeaningUpright = "堅韌、毅力、最後的考驗、警惕、累積的經驗。雖然疲憊但勝利在望，再堅持一下就能跨過最後的障礙。", MeaningReversed = "精疲力竭、放棄邊緣、過度防備、偏執。長期的壓力導致身心俱疲，需要休息和重新評估。", Keywords = ["堅韌", "毅力", "考驗", "警惕", "堅持"] },
        new() { Id = "minor_wands_10", Name = "Ten of Wands", NameCht = "權杖十", Arcana = "minor", Suit = "wands", Number = 10, MeaningUpright = "負擔過重、責任、壓力、努力、接近終點。背負著沉重的責任前行，雖然辛苦但目標已經近在眼前。", MeaningReversed = "卸下重擔、委派任務、釋放壓力、逃避責任。學習放下不必要的負擔，或將任務分配給他人。", Keywords = ["負擔", "責任", "壓力", "努力", "堅持"] },
        new() { Id = "minor_wands_11_page", Name = "Page of Wands", NameCht = "權杖侍者", Arcana = "minor", Suit = "wands", Number = 11, MeaningUpright = "探索、熱忱、好奇心、新訊息、冒險精神。充滿熱情地探索新的可能性，以開放的心態迎接冒險。", MeaningReversed = "缺乏方向、三心二意、延遲的消息、急躁。熱情無法聚焦或開始了太多事情卻無法完成。", Keywords = ["探索", "熱忱", "好奇", "訊息", "冒險"] },
        new() { Id = "minor_wands_12_knight", Name = "Knight of Wands", NameCht = "權杖騎士", Arcana = "minor", Suit = "wands", Number = 12, MeaningUpright = "冒險、衝勁、熱情、大膽行動、魅力。充滿活力地追求目標，以無畏的精神和熱情感染周圍的人。", MeaningReversed = "衝動、魯莽、挫折、缺乏耐心、方向錯誤。過於急躁而做出草率的決定，需要三思而後行。", Keywords = ["冒險", "衝勁", "熱情", "大膽", "魅力"] },
        new() { Id = "minor_wands_13_queen", Name = "Queen of Wands", NameCht = "權杖王后", Arcana = "minor", Suit = "wands", Number = 13, MeaningUpright = "自信、獨立、溫暖、決斷力、社交魅力。以優雅而強大的姿態引領他人，散發著溫暖而堅定的能量。", MeaningReversed = "嫉妒、自私、苛刻、缺乏自信、控制慾。內心的不安全感導致對他人的苛求或嫉妒心態。", Keywords = ["自信", "獨立", "溫暖", "決斷", "魅力"] },
        new() { Id = "minor_wands_14_king", Name = "King of Wands", NameCht = "權杖國王", Arcana = "minor", Suit = "wands", Number = 14, MeaningUpright = "領袖氣質、遠見、創業精神、激勵他人、行動力。以宏觀的視野和堅定的行動力帶領眾人走向成功。", MeaningReversed = "獨裁、傲慢、衝動的決策、期望過高。領導方式過於強勢，或設定了不切實際的目標。", Keywords = ["領袖", "遠見", "創業", "激勵", "行動"] },
    ];

    // ===== Minor Arcana - Cups (14 cards) =====

    private static readonly TarotCardInfo[] CupsCards =
    [
        new() { Id = "minor_cups_01_ace", Name = "Ace of Cups", NameCht = "聖杯一", Arcana = "minor", Suit = "cups", Number = 1, MeaningUpright = "新的感情、愛的開始、情感豐沛、直覺、喜悅。心靈之杯滿溢，新的情感關係或創意靈感正在萌芽。", MeaningReversed = "情感封閉、空虛、被拒絕、失落。內心的愛無法流動或情感上感到匱乏，需要重新與感受連結。", Keywords = ["新感情", "愛", "情感", "直覺", "喜悅"] },
        new() { Id = "minor_cups_02", Name = "Two of Cups", NameCht = "聖杯二", Arcana = "minor", Suit = "cups", Number = 2, MeaningUpright = "夥伴關係、相互吸引、連結、和諧、承諾。兩個靈魂之間建立起深厚的情感連結，相互理解與尊重。", MeaningReversed = "關係失衡、分離、不信任、溝通不良。兩人之間的和諧被打破，需要重新調整彼此的期待。", Keywords = ["夥伴", "吸引", "連結", "和諧", "承諾"] },
        new() { Id = "minor_cups_03", Name = "Three of Cups", NameCht = "聖杯三", Arcana = "minor", Suit = "cups", Number = 3, MeaningUpright = "友誼、歡慶、社交、創意合作、團聚。與好友們分享喜悅的時光，慶祝共同的成就與美好的關係。", MeaningReversed = "社交疲勞、流言蜚語、過度放縱、孤立。社交圈出現問題，或過度沉溺於享樂而忽略重要事務。", Keywords = ["友誼", "歡慶", "社交", "合作", "團聚"] },
        new() { Id = "minor_cups_04", Name = "Four of Cups", NameCht = "聖杯四", Arcana = "minor", Suit = "cups", Number = 4, MeaningUpright = "冷漠、不滿足、沉思、忽視機會、內省。對眼前的事物感到厭倦或不滿，沉浸在自己的世界中反思。", MeaningReversed = "新的覺知、接受機會、走出冷漠、動力恢復。開始注意到之前忽略的機會，重新燃起對生活的熱情。", Keywords = ["冷漠", "不滿", "沉思", "忽視", "內省"] },
        new() { Id = "minor_cups_05", Name = "Five of Cups", NameCht = "聖杯五", Arcana = "minor", Suit = "cups", Number = 5, MeaningUpright = "失落、悲傷、悔恨、專注於失去、哀悼。沉浸在失去的痛苦中，但別忘了身邊仍有值得珍惜的事物。", MeaningReversed = "接受、放下、走出悲傷、尋找希望。逐漸從失落中恢復，學會接受並看見生活中仍存在的美好。", Keywords = ["失落", "悲傷", "悔恨", "哀悼", "放下"] },
        new() { Id = "minor_cups_06", Name = "Six of Cups", NameCht = "聖杯六", Arcana = "minor", Suit = "cups", Number = 6, MeaningUpright = "回憶、童年、純真、懷舊、善意。溫馨的回憶湧上心頭，以純真的心態重新連結過去的美好。", MeaningReversed = "活在過去、不切實際的懷舊、拒絕成長。過度沉溺於過去而無法面對現實，需要向前看。", Keywords = ["回憶", "童年", "純真", "懷舊", "善意"] },
        new() { Id = "minor_cups_07", Name = "Seven of Cups", NameCht = "聖杯七", Arcana = "minor", Suit = "cups", Number = 7, MeaningUpright = "幻想、選擇過多、白日夢、誘惑、想像力。面對太多選擇而感到迷惘，需要分辨幻象與真實的機會。", MeaningReversed = "聚焦、做出決定、腳踏實地、看清現實。不再被幻想迷惑，開始做出清晰而實際的選擇。", Keywords = ["幻想", "選擇", "白日夢", "誘惑", "想像"] },
        new() { Id = "minor_cups_08", Name = "Eight of Cups", NameCht = "聖杯八", Arcana = "minor", Suit = "cups", Number = 8, MeaningUpright = "離開、放棄、尋找更深的意義、失望、旅程。認識到某些事物已無法滿足內心需求，勇敢地踏上新的道路。", MeaningReversed = "害怕離開、逃避、漫無目的、留戀。明知應該離開卻無法下定決心，或不斷在不同方向間徘徊。", Keywords = ["離開", "放棄", "尋找", "失望", "旅程"] },
        new() { Id = "minor_cups_09", Name = "Nine of Cups", NameCht = "聖杯九", Arcana = "minor", Suit = "cups", Number = 9, MeaningUpright = "願望實現、滿足、幸福、享受、感恩。心中的願望正在實現，享受這份來之不易的滿足與幸福感。", MeaningReversed = "不滿足、貪心、物質主義、內在空虛。外在看似擁有一切，內心卻感到空虛，需要探索真正的幸福。", Keywords = ["願望", "滿足", "幸福", "享受", "感恩"] },
        new() { Id = "minor_cups_10", Name = "Ten of Cups", NameCht = "聖杯十", Arcana = "minor", Suit = "cups", Number = 10, MeaningUpright = "美滿、家庭幸福、和諧、情感圓滿、歸屬感。情感上達到完滿的狀態，家庭和諧，關係幸福美滿。", MeaningReversed = "家庭不和、期望落空、理想幻滅、關係破裂。理想中的幸福尚未實現，或家庭關係出現裂痕。", Keywords = ["美滿", "家庭", "和諧", "圓滿", "歸屬"] },
        new() { Id = "minor_cups_11_page", Name = "Page of Cups", NameCht = "聖杯侍者", Arcana = "minor", Suit = "cups", Number = 11, MeaningUpright = "創意訊息、直覺、夢想、溫柔、情感新體驗。以開放而敏感的心態迎接新的情感體驗或創意靈感。", MeaningReversed = "情感不成熟、逃避現實、創意受阻、過於敏感。情緒波動較大，或過度沉溺在幻想中而忽略現實。", Keywords = ["創意", "直覺", "夢想", "溫柔", "敏感"] },
        new() { Id = "minor_cups_12_knight", Name = "Knight of Cups", NameCht = "聖杯騎士", Arcana = "minor", Suit = "cups", Number = 12, MeaningUpright = "浪漫、追求、魅力、想像力、理想主義。帶著浪漫的情懷追尋心中的理想，以優雅的方式表達情感。", MeaningReversed = "不切實際、情緒化、失望、虛偽的浪漫。過度理想化而脫離現實，或情感表達缺乏真誠。", Keywords = ["浪漫", "追求", "魅力", "想像", "理想"] },
        new() { Id = "minor_cups_13_queen", Name = "Queen of Cups", NameCht = "聖杯王后", Arcana = "minor", Suit = "cups", Number = 13, MeaningUpright = "同理心、直覺、情感成熟、關懷、心靈療癒。以深厚的同理心理解他人，成為他人情感上的支柱與安慰。", MeaningReversed = "情緒不穩、過度付出、依賴他人、情感操控。在照顧他人的過程中失去了自我，需要設立情感界線。", Keywords = ["同理心", "直覺", "成熟", "關懷", "療癒"] },
        new() { Id = "minor_cups_14_king", Name = "King of Cups", NameCht = "聖杯國王", Arcana = "minor", Suit = "cups", Number = 14, MeaningUpright = "情感平衡、智慧、外交手腕、包容、情緒掌控。以沉穩而有智慧的態度處理情感事務，平衡理性與感性。", MeaningReversed = "情緒壓抑、操控、冷漠、情感失控。表面平靜但內心波濤洶湧，或利用情感手段操控他人。", Keywords = ["平衡", "智慧", "外交", "包容", "掌控"] },
    ];

    // ===== Minor Arcana - Swords (14 cards) =====

    private static readonly TarotCardInfo[] SwordsCards =
    [
        new() { Id = "minor_swords_01_ace", Name = "Ace of Swords", NameCht = "寶劍一", Arcana = "minor", Suit = "swords", Number = 1, MeaningUpright = "突破、清晰、真相、新思維、心智力量。思路豁然開朗，以銳利的洞察力切入問題的核心，找到真相。", MeaningReversed = "混亂、誤解、思維阻塞、不公正。思緒混亂無法做出清晰判斷，或真相被扭曲與掩蓋。", Keywords = ["突破", "清晰", "真相", "思維", "洞察"] },
        new() { Id = "minor_swords_02", Name = "Two of Swords", NameCht = "寶劍二", Arcana = "minor", Suit = "swords", Number = 2, MeaningUpright = "抉擇困難、僵局、逃避決定、平衡、封閉。面對兩難的選擇而選擇暫時不做決定，拒絕面對現實。", MeaningReversed = "做出決定、資訊浮現、過度分析、內心衝突。終於獲得做決定所需的資訊，或被迫面對一直逃避的選擇。", Keywords = ["抉擇", "僵局", "逃避", "平衡", "封閉"] },
        new() { Id = "minor_swords_03", Name = "Three of Swords", NameCht = "寶劍三", Arcana = "minor", Suit = "swords", Number = 3, MeaningUpright = "心碎、痛苦、悲傷、背叛、失去。情感上遭受深刻的傷害，需要面對這份痛苦才能開始療癒。", MeaningReversed = "療癒、釋放、原諒、走出痛苦。傷口逐漸癒合，學會放下過去的傷痛並從中成長。", Keywords = ["心碎", "痛苦", "悲傷", "背叛", "失去"] },
        new() { Id = "minor_swords_04", Name = "Four of Swords", NameCht = "寶劍四", Arcana = "minor", Suit = "swords", Number = 4, MeaningUpright = "休息、恢復、冥想、退隱、充電。身心需要休息的時候到了，暫時退離紛擾，讓自己恢復元氣。", MeaningReversed = "不安、被迫恢復、精疲力竭、無法放鬆。雖然知道需要休息，但內心的焦慮使人無法真正放鬆。", Keywords = ["休息", "恢復", "冥想", "退隱", "充電"] },
        new() { Id = "minor_swords_05", Name = "Five of Swords", NameCht = "寶劍五", Arcana = "minor", Suit = "swords", Number = 5, MeaningUpright = "衝突、勝之不武、損人利己、爭執、自私。贏了戰鬥卻可能失去更重要的東西，值得反思勝利的代價。", MeaningReversed = "和解、放下爭執、認輸、學到教訓。意識到持續爭鬥的無意義，選擇放下並從經驗中學習。", Keywords = ["衝突", "爭執", "自私", "代價", "反思"] },
        new() { Id = "minor_swords_06", Name = "Six of Swords", NameCht = "寶劍六", Arcana = "minor", Suit = "swords", Number = 6, MeaningUpright = "過渡、離開困境、轉變、旅行、心靈轉化。離開動盪的環境，向著更平靜的水域航行，逐漸走向安寧。", MeaningReversed = "無法離開、抗拒改變、舊問題未解、停滯。明知需要改變卻無法踏出第一步，或帶著未解的問題前行。", Keywords = ["過渡", "離開", "轉變", "旅行", "安寧"] },
        new() { Id = "minor_swords_07", Name = "Seven of Swords", NameCht = "寶劍七", Arcana = "minor", Suit = "swords", Number = 7, MeaningUpright = "策略、欺騙、隱瞞、獨自行動、智取。需要運用策略而非正面衝突來達成目標，但要注意誠信問題。", MeaningReversed = "謊言被揭穿、良心不安、坦白、歸還。隱瞞的事情即將曝光，是時候面對真相並做出誠實的選擇。", Keywords = ["策略", "欺騙", "隱瞞", "獨行", "智取"] },
        new() { Id = "minor_swords_08", Name = "Eight of Swords", NameCht = "寶劍八", Arcana = "minor", Suit = "swords", Number = 8, MeaningUpright = "受困、限制、無力感、自我設限、焦慮。感覺被困在困境中動彈不得，但多數限制其實是自己造成的。", MeaningReversed = "解脫、新視角、重獲自由、自我解放。意識到束縛自己的是心理障礙，開始找到掙脫的方法。", Keywords = ["受困", "限制", "無力", "自我設限", "焦慮"] },
        new() { Id = "minor_swords_09", Name = "Nine of Swords", NameCht = "寶劍九", Arcana = "minor", Suit = "swords", Number = 9, MeaningUpright = "焦慮、噩夢、擔憂、失眠、內心折磨。深夜的恐懼與擔憂侵蝕心靈，但很多恐懼比實際情況更糟。", MeaningReversed = "走出焦慮、面對恐懼、尋求幫助、轉念。開始意識到擔憂無益，勇敢面對內心的恐懼並尋求支持。", Keywords = ["焦慮", "噩夢", "擔憂", "失眠", "恐懼"] },
        new() { Id = "minor_swords_10", Name = "Ten of Swords", NameCht = "寶劍十", Arcana = "minor", Suit = "swords", Number = 10, MeaningUpright = "終結、崩潰、背叛、觸底、結束的痛苦。最黑暗的時刻已經到來，但黎明就在前方，一切只會從這裡好轉。", MeaningReversed = "復甦、最壞已過、倖存、重新振作。已經度過了最困難的時期，開始慢慢站起來重新開始。", Keywords = ["終結", "崩潰", "觸底", "結束", "重生"] },
        new() { Id = "minor_swords_11_page", Name = "Page of Swords", NameCht = "寶劍侍者", Arcana = "minor", Suit = "swords", Number = 11, MeaningUpright = "好奇心、求知慾、新想法、觀察力、直言不諱。以敏銳的觀察力和求知慾探索周圍的世界。", MeaningReversed = "流言蜚語、刻薄、思緒混亂、缺乏深度。言語傷人或過於表面化，需要更深入的思考。", Keywords = ["好奇", "求知", "觀察", "直言", "敏銳"] },
        new() { Id = "minor_swords_12_knight", Name = "Knight of Swords", NameCht = "寶劍騎士", Arcana = "minor", Suit = "swords", Number = 12, MeaningUpright = "果斷、快速行動、直接、雄心勃勃、聰明。以迅雷不及掩耳之勢採取行動，思維敏捷且目標明確。", MeaningReversed = "衝動、缺乏方向、無禮、散漫。行動太快而未經深思，或言辭過於尖銳傷害了他人。", Keywords = ["果斷", "快速", "直接", "雄心", "敏捷"] },
        new() { Id = "minor_swords_13_queen", Name = "Queen of Swords", NameCht = "寶劍王后", Arcana = "minor", Suit = "swords", Number = 13, MeaningUpright = "獨立思考、清晰判斷、客觀公正、經驗智慧、直話直說。以冷靜而公正的態度面對問題，不被情感左右。", MeaningReversed = "冷酷、尖酸、偏見、情感壓抑。過度理性而顯得冷漠無情，或以嚴苛的標準評判他人。", Keywords = ["獨立", "判斷", "公正", "智慧", "直率"] },
        new() { Id = "minor_swords_14_king", Name = "King of Swords", NameCht = "寶劍國王", Arcana = "minor", Suit = "swords", Number = 14, MeaningUpright = "理性權威、公正裁決、知識淵博、分析力、真理。以理性和公正為準則做出明智的決策，堅持真理。", MeaningReversed = "濫用權力、操縱、冷酷無情、獨斷。用知識和權力來壓迫他人，或做出缺乏同理心的決定。", Keywords = ["理性", "公正", "知識", "分析", "真理"] },
    ];

    // ===== Minor Arcana - Pentacles (14 cards) =====

    private static readonly TarotCardInfo[] PentaclesCards =
    [
        new() { Id = "minor_pentacles_01_ace", Name = "Ace of Pentacles", NameCht = "錢幣一", Arcana = "minor", Suit = "pentacles", Number = 1, MeaningUpright = "新的財務機會、物質豐盛、繁榮的開端、實際收穫。一個能帶來實質回報的新機會正在出現，把握時機。", MeaningReversed = "錯失機會、財務損失、計畫落空、缺乏安全感。承諾的回報未能實現，或因猶豫而錯過重要機會。", Keywords = ["機會", "豐盛", "繁榮", "收穫", "物質"] },
        new() { Id = "minor_pentacles_02", Name = "Two of Pentacles", NameCht = "錢幣二", Arcana = "minor", Suit = "pentacles", Number = 2, MeaningUpright = "平衡、適應、多工處理、靈活變通、優先排序。在多項事務之間靈活調配，保持動態的平衡。", MeaningReversed = "失衡、分身乏術、財務混亂、優先順序錯誤。同時處理太多事情導致顧此失彼，需要重新排序。", Keywords = ["平衡", "適應", "多工", "靈活", "排序"] },
        new() { Id = "minor_pentacles_03", Name = "Three of Pentacles", NameCht = "錢幣三", Arcana = "minor", Suit = "pentacles", Number = 3, MeaningUpright = "團隊合作、技藝精進、學習、品質、規劃。透過專業技能和團隊協作，建造出高品質的成果。", MeaningReversed = "缺乏合作、品質低落、偷工減料、不被認可。團隊配合出現問題，或努力未獲得應有的肯定。", Keywords = ["合作", "技藝", "學習", "品質", "規劃"] },
        new() { Id = "minor_pentacles_04", Name = "Four of Pentacles", NameCht = "錢幣四", Arcana = "minor", Suit = "pentacles", Number = 4, MeaningUpright = "保守、儲蓄、安全感、控制、穩定。緊握手中的資源不願放手，重視財務安全與物質穩定。", MeaningReversed = "貪婪、過度執著、慷慨、放手。過度守財反而限制了成長，或終於學會適度地分享與放手。", Keywords = ["保守", "儲蓄", "安全", "控制", "穩定"] },
        new() { Id = "minor_pentacles_05", Name = "Five of Pentacles", NameCht = "錢幣五", Arcana = "minor", Suit = "pentacles", Number = 5, MeaningUpright = "困難、貧困、孤立、健康問題、缺乏支援。經歷物質或精神上的匱乏，但援助可能就在附近。", MeaningReversed = "恢復、走出困境、接受幫助、改善。最困難的時期正在過去，開始接受他人的支持與幫助。", Keywords = ["困難", "貧困", "孤立", "匱乏", "援助"] },
        new() { Id = "minor_pentacles_06", Name = "Six of Pentacles", NameCht = "錢幣六", Arcana = "minor", Suit = "pentacles", Number = 6, MeaningUpright = "慷慨、施與受、分享、公平交易、善行。處於能夠幫助他人的位置，慷慨地分享自己的資源。", MeaningReversed = "施恩圖報、不平等、債務、自私。給予帶有附加條件，或權力關係不平等導致的利用。", Keywords = ["慷慨", "施與受", "分享", "公平", "善行"] },
        new() { Id = "minor_pentacles_07", Name = "Seven of Pentacles", NameCht = "錢幣七", Arcana = "minor", Suit = "pentacles", Number = 7, MeaningUpright = "耐心等待、長期投資、評估成果、反思、收成在望。播下的種子正在生長，需要耐心等待收穫的時刻。", MeaningReversed = "缺乏耐心、回報不如預期、方向錯誤、浪費。長期投入的努力似乎沒有帶來預期成果，需要重新評估。", Keywords = ["耐心", "投資", "評估", "反思", "等待"] },
        new() { Id = "minor_pentacles_08", Name = "Eight of Pentacles", NameCht = "錢幣八", Arcana = "minor", Suit = "pentacles", Number = 8, MeaningUpright = "勤勉、精進技藝、專注、學徒精神、品質追求。全心投入工作或學習中，透過不斷練習精進自己的技能。", MeaningReversed = "敷衍了事、缺乏專注、完美主義、重複勞動。對工作失去熱情，或過於追求完美而無法前進。", Keywords = ["勤勉", "精進", "專注", "學習", "品質"] },
        new() { Id = "minor_pentacles_09", Name = "Nine of Pentacles", NameCht = "錢幣九", Arcana = "minor", Suit = "pentacles", Number = 9, MeaningUpright = "富足、自給自足、優雅、成就、獨立。憑藉自己的努力達到了物質與精神上的富足，享受獨立自主的生活。", MeaningReversed = "過度消費、依賴他人、表面光鮮、缺乏安全感。外表看似富足但內心不安，或過度揮霍導致不穩定。", Keywords = ["富足", "自給自足", "優雅", "成就", "獨立"] },
        new() { Id = "minor_pentacles_10", Name = "Ten of Pentacles", NameCht = "錢幣十", Arcana = "minor", Suit = "pentacles", Number = 10, MeaningUpright = "家族財富、傳承、長久穩定、退休、圓滿。物質上的長期安穩與家族的繁榮，代代相傳的財富與價值。", MeaningReversed = "家族紛爭、財務危機、失去傳承、不穩定的基礎。家庭在財務或價值觀上產生分歧，動搖了穩定的根基。", Keywords = ["財富", "傳承", "穩定", "家族", "圓滿"] },
        new() { Id = "minor_pentacles_11_page", Name = "Page of Pentacles", NameCht = "錢幣侍者", Arcana = "minor", Suit = "pentacles", Number = 11, MeaningUpright = "學習機會、新技能、踏實、好奇探索、規劃。以認真踏實的態度學習新技能，為未來的成功奠定基礎。", MeaningReversed = "缺乏進展、不切實際、分心、錯過學習機會。缺乏專注力或方向感，無法將想法轉化為實際行動。", Keywords = ["學習", "技能", "踏實", "探索", "規劃"] },
        new() { Id = "minor_pentacles_12_knight", Name = "Knight of Pentacles", NameCht = "錢幣騎士", Arcana = "minor", Suit = "pentacles", Number = 12, MeaningUpright = "勤奮、可靠、有條不紊、耐心、責任感。以穩定而有條理的方式推進目標，雖然緩慢但確實可靠。", MeaningReversed = "固執、無聊、過於保守、停滯不前。過度謹慎而錯失機會，或陷入一成不變的生活模式。", Keywords = ["勤奮", "可靠", "有條理", "耐心", "責任"] },
        new() { Id = "minor_pentacles_13_queen", Name = "Queen of Pentacles", NameCht = "錢幣王后", Arcana = "minor", Suit = "pentacles", Number = 13, MeaningUpright = "務實、豐盛、照顧、理財有方、生活品質。以務實而溫暖的方式經營生活，創造舒適而豐盛的環境。", MeaningReversed = "過度物質、忽視自我、財務擔憂、失去平衡。過度關注物質層面而忽略了精神需求和自我照顧。", Keywords = ["務實", "豐盛", "照顧", "理財", "品質"] },
        new() { Id = "minor_pentacles_14_king", Name = "King of Pentacles", NameCht = "錢幣國王", Arcana = "minor", Suit = "pentacles", Number = 14, MeaningUpright = "財富、事業成功、穩健領導、豐盛、實際智慧。透過穩健經營和實際智慧累積了豐厚的財富與地位。", MeaningReversed = "貪婪、守財奴、物質至上、濫用資源。過度追求物質利益而失去了人情味，或管理不善導致損失。", Keywords = ["財富", "成功", "穩健", "豐盛", "智慧"] },
    ];

    private static readonly TarotCardInfo[] MinorArcana =
        [.. WandsCards, .. CupsCards, .. SwordsCards, .. PentaclesCards];
}
