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

const threeCardSpread: SpreadConfig = {
	type: 'three-card',
	name: '三張牌',
	description: '過去、現在、未來的時間線展開。',
	cardCount: 3,
	positions: [
		{ index: 0, label: '過去', description: '影響當前情況的過去因素' },
		{ index: 1, label: '現在', description: '目前的狀態與挑戰' },
		{ index: 2, label: '未來', description: '如果沿著目前道路前進的可能發展' }
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

export const spreadConfigs: Record<SpreadType, SpreadConfig> = {
	single: singleSpread,
	'three-card': threeCardSpread,
	'celtic-cross': celticCrossSpread
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

	const shuffled = secureShuffle(allCards);
	const selected = shuffled.slice(0, config.cardCount);

	const cards: DrawnCard[] = selected.map((card, i) => ({
		card,
		orientation: randomOrientation(),
		position: config.positions[i]
	}));

	return {
		spreadType,
		question,
		cards,
		createdAt: new Date().toISOString()
	};
}
