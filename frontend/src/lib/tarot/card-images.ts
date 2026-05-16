// Rider-Waite-Smith tarot card images (Public Domain).
// Source: https://commons.wikimedia.org/wiki/Category:Rider-Waite_tarot_deck
//
// The 78 images are stored locally under `static/cards/<cardId>.jpg` so the app
// no longer depends on Wikimedia's CDN at runtime (which intermittently
// rate-limited / 403'd hotlinked requests). Files were fetched once at build
// time; see git history for the original Wikimedia URLs.

const LOCAL_BASE = '/cards';

// Every card id maps 1:1 to `static/cards/<id>.jpg`. The full id list is the
// 78-card deck defined in `cards.ts` — kept here so an unknown id returns
// undefined (triggering the fallback render) instead of a broken image.
const cardIds = new Set<string>([
	// === Major Arcana ===
	'major_00_fool',
	'major_01_magician',
	'major_02_high_priestess',
	'major_03_empress',
	'major_04_emperor',
	'major_05_hierophant',
	'major_06_lovers',
	'major_07_chariot',
	'major_08_strength',
	'major_09_hermit',
	'major_10_wheel_of_fortune',
	'major_11_justice',
	'major_12_hanged_man',
	'major_13_death',
	'major_14_temperance',
	'major_15_devil',
	'major_16_tower',
	'major_17_star',
	'major_18_moon',
	'major_19_sun',
	'major_20_judgement',
	'major_21_world',
	// === Minor Arcana - Wands ===
	'minor_wands_01_ace',
	'minor_wands_02',
	'minor_wands_03',
	'minor_wands_04',
	'minor_wands_05',
	'minor_wands_06',
	'minor_wands_07',
	'minor_wands_08',
	'minor_wands_09',
	'minor_wands_10',
	'minor_wands_11_page',
	'minor_wands_12_knight',
	'minor_wands_13_queen',
	'minor_wands_14_king',
	// === Minor Arcana - Cups ===
	'minor_cups_01_ace',
	'minor_cups_02',
	'minor_cups_03',
	'minor_cups_04',
	'minor_cups_05',
	'minor_cups_06',
	'minor_cups_07',
	'minor_cups_08',
	'minor_cups_09',
	'minor_cups_10',
	'minor_cups_11_page',
	'minor_cups_12_knight',
	'minor_cups_13_queen',
	'minor_cups_14_king',
	// === Minor Arcana - Swords ===
	'minor_swords_01_ace',
	'minor_swords_02',
	'minor_swords_03',
	'minor_swords_04',
	'minor_swords_05',
	'minor_swords_06',
	'minor_swords_07',
	'minor_swords_08',
	'minor_swords_09',
	'minor_swords_10',
	'minor_swords_11_page',
	'minor_swords_12_knight',
	'minor_swords_13_queen',
	'minor_swords_14_king',
	// === Minor Arcana - Pentacles ===
	'minor_pentacles_01_ace',
	'minor_pentacles_02',
	'minor_pentacles_03',
	'minor_pentacles_04',
	'minor_pentacles_05',
	'minor_pentacles_06',
	'minor_pentacles_07',
	'minor_pentacles_08',
	'minor_pentacles_09',
	'minor_pentacles_10',
	'minor_pentacles_11_page',
	'minor_pentacles_12_knight',
	'minor_pentacles_13_queen',
	'minor_pentacles_14_king'
]);

export function getCardImageUrl(cardId: string): string | undefined {
	return cardIds.has(cardId) ? `${LOCAL_BASE}/${cardId}.jpg` : undefined;
}
