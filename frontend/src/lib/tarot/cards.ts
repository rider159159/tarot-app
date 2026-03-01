import type { TarotCard } from '$lib/types';
import { majorArcana } from './major-arcana';
import { minorArcana } from './minor-arcana';

export const allCards: TarotCard[] = [...majorArcana, ...minorArcana];

export const cardById: Map<string, TarotCard> = new Map(
	allCards.map((card) => [card.id, card])
);

export function getCardById(id: string): TarotCard {
	const card = cardById.get(id);
	if (!card) {
		throw new Error(`Card not found: ${id}`);
	}
	return card;
}

export { majorArcana, minorArcana };
