import type {
	SpreadConfig,
	SpreadType,
	DrawnCard,
	Orientation,
	ReadingResult
} from '$lib/types';
import { allCards } from './cards';

// === Spread Definitions ===

const singleSpread: SpreadConfig = {
	type: 'single',
	name: '單張牌',
	description: '抽取一張牌，適合每日指引或簡單問題。',
	cardCount: 1,
	positions: [{ index: 0, label: '指引', description: '此刻對你最重要的訊息' }]
};

const threeCardTimeSpread: SpreadConfig = {
	type: 'three-card-time',
	name: '三張牌 — 時間流',
	description: '過去、現在、未來的時間線展開。',
	cardCount: 3,
	positions: [
		{ index: 0, label: '過去', description: '影響當前情況的過去因素' },
		{ index: 1, label: '現在', description: '目前的狀態與挑戰' },
		{ index: 2, label: '未來', description: '如果沿著目前道路前進的可能發展' }
	]
};

const threeCardProblemSpread: SpreadConfig = {
	type: 'three-card-problem',
	name: '三張牌 — 問題對策',
	description: '分析問題的核心、原因與解決方向。',
	cardCount: 3,
	positions: [
		{ index: 0, label: '問題', description: '你目前面對的核心問題' },
		{ index: 1, label: '原因', description: '造成這個問題的根本原因' },
		{ index: 2, label: '對策', description: '可能的解決方向與建議' }
	]
};

const threeCardLinearSpread: SpreadConfig = {
	type: 'three-card-linear',
	name: '三張牌 — 線性展開',
	description: '三張牌依序展開，不帶預設含義。',
	cardCount: 3,
	positions: [
		{ index: 0, label: '第一張', description: '牌陣中的第一個訊息' },
		{ index: 1, label: '第二張', description: '牌陣中的第二個訊息' },
		{ index: 2, label: '第三張', description: '牌陣中的第三個訊息' }
	]
};

const celticCrossSpread: SpreadConfig = {
	type: 'celtic-cross',
	name: '塞爾特十字',
	description: '最經典的十張牌牌陣，全面深入分析問題。',
	cardCount: 10,
	positions: [
		{ index: 0, label: '現狀', description: '目前的處境與核心問題' },
		{ index: 1, label: '挑戰', description: '當前面臨的阻礙或對立力量' },
		{ index: 2, label: '潛意識', description: '內心深處的想法與潛在影響' },
		{ index: 3, label: '過去', description: '近期影響事件發展的過去因素' },
		{ index: 4, label: '可能性', description: '最佳可能結果或目標' },
		{ index: 5, label: '近未來', description: '即將發生的事件或影響' },
		{ index: 6, label: '自我', description: '你對這個問題的態度與立場' },
		{ index: 7, label: '環境', description: '周圍環境與他人的影響' },
		{ index: 8, label: '希望與恐懼', description: '內心的期望或擔憂' },
		{ index: 9, label: '結果', description: '最終可能的結果' }
	]
};

const weeklyFortuneSpread: SpreadConfig = {
	type: 'weekly-fortune',
	name: '每週週運',
	description: '抽取三張牌，看看本週的能量、挑戰與建議。',
	cardCount: 3,
	positions: [
		{ index: 0, label: '本週能量', description: '這週圍繞你的整體能量與氛圍' },
		{ index: 1, label: '本週挑戰', description: '這週可能面臨的挑戰或需要注意的事項' },
		{ index: 2, label: '本週建議', description: '面對本週的指引與建議' }
	]
};

export const spreadConfigs: Record<SpreadType, SpreadConfig> = {
	single: singleSpread,
	'three-card-time': threeCardTimeSpread,
	'three-card-problem': threeCardProblemSpread,
	'three-card-linear': threeCardLinearSpread,
	'celtic-cross': celticCrossSpread,
	'weekly-fortune': weeklyFortuneSpread
};

// === Crypto-based Random Utilities ===

function secureRandomInt(max: number): number {
	if (max <= 0) throw new Error('max must be positive');
	if (max === 1) return 0;

	const array = new Uint32Array(1);
	const limit = Math.floor(0x100000000 / max) * max;

	let value: number;
	do {
		crypto.getRandomValues(array);
		value = array[0];
	} while (value >= limit);

	return value % max;
}

function secureShuffle<T>(items: readonly T[]): T[] {
	const result = [...items];
	for (let i = result.length - 1; i > 0; i--) {
		const j = secureRandomInt(i + 1);
		[result[i], result[j]] = [result[j], result[i]];
	}
	return result;
}

function randomOrientation(): Orientation {
	const array = new Uint8Array(1);
	crypto.getRandomValues(array);
	return array[0] & 1 ? 'upright' : 'reversed';
}

// === Drawing Function ===

export function drawCards(spreadType: SpreadType, question: string): ReadingResult {
	const config = spreadConfigs[spreadType];
	if (!config) {
		throw new Error(`Unknown spread type: ${spreadType}`);
	}

	const needsFeeling = spreadType !== 'single';
	const totalCards = needsFeeling ? config.cardCount + 1 : config.cardCount;
	const shuffled = secureShuffle(allCards);
	const selected = shuffled.slice(0, totalCards);

	const cards: DrawnCard[] = selected.slice(0, config.cardCount).map((card, i) => ({
		card,
		orientation: randomOrientation(),
		position: config.positions[i]
	}));

	if (needsFeeling) {
		cards.push({
			card: selected[config.cardCount],
			orientation: randomOrientation(),
			position: {
				index: config.cardCount,
				label: '你的感受',
				description: '你對此問題最真實的內心感受'
			}
		});
	}

	return {
		spreadType,
		question,
		cards,
		createdAt: new Date().toISOString()
	};
}
