import type { ApiReadingResponse, DrawnCard, ReadingResult, SpreadType } from '$lib/types';

export function mapApiResponse(res: ApiReadingResponse): ReadingResult {
	const cards: DrawnCard[] = res.cards.map((c) => ({
		card: {
			id: c.cardId,
			name: c.name,
			nameCht: c.nameCht,
			arcana: c.arcana as 'major' | 'minor',
			suit: c.suit as 'wands' | 'cups' | 'swords' | 'pentacles' | undefined,
			number: 0,
			meaningUpright: c.orientation === 'upright' ? c.meaning : '',
			meaningReversed: c.orientation === 'reversed' ? c.meaning : '',
			keywords: c.keywords
		},
		orientation: c.orientation,
		position: {
			index: c.positionIndex,
			label: c.positionLabel,
			description: c.positionDescription
		}
	}));

	return {
		id: res.id,
		spreadType: res.spreadType as SpreadType,
		question: res.question ?? '',
		cards,
		createdAt: res.createdAt
	};
}

const spreadNameMap: Record<string, string> = {
	single: '單張牌',
	'three-card': '三張牌',
	'celtic-cross': '塞爾特十字'
};

export function getSpreadName(spreadType: string): string {
	return spreadNameMap[spreadType] ?? spreadType;
}

export function formatDate(dateStr: string): string {
	return new Date(dateStr).toLocaleString('zh-TW', {
		year: 'numeric',
		month: 'long',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit'
	});
}
