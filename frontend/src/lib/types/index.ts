export type Suit = 'wands' | 'cups' | 'swords' | 'pentacles';
export type Arcana = 'major' | 'minor';

export interface TarotCard {
	id: string;
	name: string;
	nameCht: string;
	arcana: Arcana;
	suit?: Suit;
	number: number;
	meaningUpright: string;
	meaningReversed: string;
	keywords: string[];
}

export type SpreadType = 'single' | 'three-card' | 'celtic-cross';

export interface SpreadPosition {
	index: number;
	label: string;
	description: string;
}

export interface SpreadConfig {
	type: SpreadType;
	name: string;
	description: string;
	cardCount: number;
	positions: SpreadPosition[];
}

export type Orientation = 'upright' | 'reversed';

export interface DrawnCard {
	card: TarotCard;
	orientation: Orientation;
	position: SpreadPosition;
}

export interface ReadingResult {
	id?: string;
	spreadType: SpreadType;
	question: string;
	cards: DrawnCard[];
	createdAt: string;
}

export interface Profile {
	id: string;
	display_name: string;
	created_at: string;
	updated_at: string;
}

export interface ReadingRow {
	id: string;
	user_id: string;
	spread_type: SpreadType;
	question: string;
	cards: DrawnCardRow[];
	interpretation?: string;
	notes?: string;
	created_at: string;
}

export interface DrawnCardRow {
	card_id: string;
	orientation: Orientation;
	position_index: number;
}

// === API Response Types (from .NET backend) ===

export interface ApiDrawnCard {
	cardId: string;
	name: string;
	nameCht: string;
	arcana: string;
	suit?: string;
	orientation: Orientation;
	meaning: string;
	keywords: string[];
	positionIndex: number;
	positionLabel: string;
	positionDescription: string;
}

export interface ApiReadingResponse {
	id: string;
	spreadType: string;
	question?: string;
	cards: ApiDrawnCard[];
	interpretation?: string;
	notes?: string;
	createdAt: string;
}

export interface ApiReadingsPage {
	items: ApiReadingResponse[];
	totalCount: number;
	page: number;
	pageSize: number;
}
