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

export type SpreadType =
	| 'single'
	| 'three-card-time'
	| 'three-card-problem'
	| 'three-card-linear'
	| 'celtic-cross'
	| 'weekly-fortune'
	| 'custom';

// Spread types that have a fixed, preset configuration in `spreadConfigs`.
// 'custom' is excluded — its card count is chosen at draw time.
export type FixedSpreadType = Exclude<SpreadType, 'custom'>;

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

// Anonymous draw — same shape minus persistence fields. The client owns
// the resulting clientToken and stores it in localStorage alongside this.
export interface ApiAnonymousDrawResponse {
	spreadType: string;
	question?: string;
	drawnAt: string;
	cards: ApiDrawnCard[];
}

export interface ApiReadingsPage {
	items: ApiReadingResponse[];
	totalCount: number;
	page: number;
	pageSize: number;
}

// === Profile API Types ===

export interface ApiProfileResponse {
	id: string;
	displayName: string;
	createdAt: string;
}

export interface ApiProfileUpdateRequest {
	displayName: string;
}

// === Weekly Fortune API Types ===

export interface ApiWeeklyFortuneResponse {
	reading: ApiReadingResponse | null;
	canDraw: boolean;
}

// === Stats API Types ===

export interface ApiCardStat {
	cardId: string;
	nameCht: string;
	count: number;
}

export interface ApiSpreadStat {
	spreadType: string;
	count: number;
}

export interface ApiReadingStatsResponse {
	totalCount: number;
	topCards: ApiCardStat[];
	spreadUsage: ApiSpreadStat[];
	lastReadingAt: string | null;
}

// === Export API Types ===

export interface ApiExportCard {
	position: string;
	name: string;
	nameEn: string;
	orientation: string;
	keywords: string[];
	meaning: string;
}

export interface ApiExportReading {
	id: string;
	drawnAt: string;
	spreadType: string;
	spreadTypeKey: string;
	question: string | null;
	cards: ApiExportCard[];
}

export interface ApiExportMetadata {
	exportedAt: string;
	source: string;
	formatVersion: string;
}

export interface ApiExportPayload {
	metadata: ApiExportMetadata;
	reading: ApiExportReading;
	aiPromptSuggestion: string;
}

export interface ApiExportBatch {
	metadata: ApiExportMetadata;
	totalCount: number;
	exportedCount: number;
	isTruncated: boolean;
	readings: ApiExportReading[];
}
