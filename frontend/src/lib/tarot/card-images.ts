// Rider-Waite-Smith tarot card images from Wikimedia Commons (Public Domain)
// Source: https://commons.wikimedia.org/wiki/Category:Rider-Waite_tarot_deck

const WIKI_BASE = 'https://upload.wikimedia.org/wikipedia/commons';

// cardId → Wikimedia Commons image path
const cardImagePaths: Record<string, string> = {
	// === Major Arcana ===
	major_00_fool: `${WIKI_BASE}/9/90/RWS_Tarot_00_Fool.jpg`,
	major_01_magician: `${WIKI_BASE}/d/de/RWS_Tarot_01_Magician.jpg`,
	major_02_high_priestess: `${WIKI_BASE}/8/88/RWS_Tarot_02_High_Priestess.jpg`,
	major_03_empress: `${WIKI_BASE}/d/d2/RWS_Tarot_03_Empress.jpg`,
	major_04_emperor: `${WIKI_BASE}/c/c3/RWS_Tarot_04_Emperor.jpg`,
	major_05_hierophant: `${WIKI_BASE}/8/8d/RWS_Tarot_05_Hierophant.jpg`,
	major_06_lovers: `${WIKI_BASE}/3/3a/RWS_Tarot_06_Lovers.jpg`,
	major_07_chariot: `${WIKI_BASE}/9/9b/RWS_Tarot_07_Chariot.jpg`,
	major_08_strength: `${WIKI_BASE}/f/f5/RWS_Tarot_08_Strength.jpg`,
	major_09_hermit: `${WIKI_BASE}/4/4d/RWS_Tarot_09_Hermit.jpg`,
	major_10_wheel_of_fortune: `${WIKI_BASE}/3/3c/RWS_Tarot_10_Wheel_of_Fortune.jpg`,
	major_11_justice: `${WIKI_BASE}/e/e0/RWS_Tarot_11_Justice.jpg`,
	major_12_hanged_man: `${WIKI_BASE}/2/2b/RWS_Tarot_12_Hanged_Man.jpg`,
	major_13_death: `${WIKI_BASE}/d/d7/RWS_Tarot_13_Death.jpg`,
	major_14_temperance: `${WIKI_BASE}/f/f8/RWS_Tarot_14_Temperance.jpg`,
	major_15_devil: `${WIKI_BASE}/5/55/RWS_Tarot_15_Devil.jpg`,
	major_16_tower: `${WIKI_BASE}/5/53/RWS_Tarot_16_Tower.jpg`,
	major_17_star: `${WIKI_BASE}/d/db/RWS_Tarot_17_Star.jpg`,
	major_18_moon: `${WIKI_BASE}/7/7f/RWS_Tarot_18_Moon.jpg`,
	major_19_sun: `${WIKI_BASE}/1/17/RWS_Tarot_19_Sun.jpg`,
	major_20_judgement: `${WIKI_BASE}/d/dd/RWS_Tarot_20_Judgement.jpg`,
	major_21_world: `${WIKI_BASE}/f/ff/RWS_Tarot_21_World.jpg`,

	// === Minor Arcana - Wands ===
	minor_wands_01_ace: `${WIKI_BASE}/1/11/Wands01.jpg`,
	minor_wands_02: `${WIKI_BASE}/0/0f/Wands02.jpg`,
	minor_wands_03: `${WIKI_BASE}/f/ff/Wands03.jpg`,
	minor_wands_04: `${WIKI_BASE}/a/a4/Wands04.jpg`,
	minor_wands_05: `${WIKI_BASE}/9/9d/Wands05.jpg`,
	minor_wands_06: `${WIKI_BASE}/3/3b/Wands06.jpg`,
	minor_wands_07: `${WIKI_BASE}/e/e4/Wands07.jpg`,
	minor_wands_08: `${WIKI_BASE}/6/6a/Wands08.jpg`,
	minor_wands_09: `${WIKI_BASE}/e/e7/Wands09.jpg`,
	minor_wands_10: `${WIKI_BASE}/0/0b/Wands10.jpg`,
	minor_wands_11_page: `${WIKI_BASE}/6/6a/Wands11.jpg`,
	minor_wands_12_knight: `${WIKI_BASE}/1/16/Wands12.jpg`,
	minor_wands_13_queen: `${WIKI_BASE}/0/0d/Wands13.jpg`,
	minor_wands_14_king: `${WIKI_BASE}/c/ce/Wands14.jpg`,

	// === Minor Arcana - Cups ===
	minor_cups_01_ace: `${WIKI_BASE}/3/36/Cups01.jpg`,
	minor_cups_02: `${WIKI_BASE}/f/f8/Cups02.jpg`,
	minor_cups_03: `${WIKI_BASE}/7/7a/Cups03.jpg`,
	minor_cups_04: `${WIKI_BASE}/3/35/Cups04.jpg`,
	minor_cups_05: `${WIKI_BASE}/d/d7/Cups05.jpg`,
	minor_cups_06: `${WIKI_BASE}/1/17/Cups06.jpg`,
	minor_cups_07: `${WIKI_BASE}/a/ae/Cups07.jpg`,
	minor_cups_08: `${WIKI_BASE}/6/60/Cups08.jpg`,
	minor_cups_09: `${WIKI_BASE}/2/24/Cups09.jpg`,
	minor_cups_10: `${WIKI_BASE}/8/84/Cups10.jpg`,
	minor_cups_11_page: `${WIKI_BASE}/a/ad/Cups11.jpg`,
	minor_cups_12_knight: `${WIKI_BASE}/f/fa/Cups12.jpg`,
	minor_cups_13_queen: `${WIKI_BASE}/6/62/Cups13.jpg`,
	minor_cups_14_king: `${WIKI_BASE}/0/04/Cups14.jpg`,

	// === Minor Arcana - Swords ===
	minor_swords_01_ace: `${WIKI_BASE}/1/1a/Swords01.jpg`,
	minor_swords_02: `${WIKI_BASE}/9/9e/Swords02.jpg`,
	minor_swords_03: `${WIKI_BASE}/0/02/Swords03.jpg`,
	minor_swords_04: `${WIKI_BASE}/b/bf/Swords04.jpg`,
	minor_swords_05: `${WIKI_BASE}/2/23/Swords05.jpg`,
	minor_swords_06: `${WIKI_BASE}/2/29/Swords06.jpg`,
	minor_swords_07: `${WIKI_BASE}/3/34/Swords07.jpg`,
	minor_swords_08: `${WIKI_BASE}/a/a7/Swords08.jpg`,
	minor_swords_09: `${WIKI_BASE}/2/2f/Swords09.jpg`,
	minor_swords_10: `${WIKI_BASE}/d/d4/Swords10.jpg`,
	minor_swords_11_page: `${WIKI_BASE}/4/4c/Swords11.jpg`,
	minor_swords_12_knight: `${WIKI_BASE}/b/b0/Swords12.jpg`,
	minor_swords_13_queen: `${WIKI_BASE}/d/d4/Swords13.jpg`,
	minor_swords_14_king: `${WIKI_BASE}/3/33/Swords14.jpg`,

	// === Minor Arcana - Pentacles ===
	minor_pentacles_01_ace: `${WIKI_BASE}/f/fd/Pents01.jpg`,
	minor_pentacles_02: `${WIKI_BASE}/9/9f/Pents02.jpg`,
	minor_pentacles_03: `${WIKI_BASE}/4/42/Pents03.jpg`,
	minor_pentacles_04: `${WIKI_BASE}/3/35/Pents04.jpg`,
	minor_pentacles_05: `${WIKI_BASE}/9/96/Pents05.jpg`,
	minor_pentacles_06: `${WIKI_BASE}/a/a6/Pents06.jpg`,
	minor_pentacles_07: `${WIKI_BASE}/6/6a/Pents07.jpg`,
	minor_pentacles_08: `${WIKI_BASE}/4/49/Pents08.jpg`,
	minor_pentacles_09: `${WIKI_BASE}/f/f0/Pents09.jpg`,
	minor_pentacles_10: `${WIKI_BASE}/4/42/Pents10.jpg`,
	minor_pentacles_11_page: `${WIKI_BASE}/e/ec/Pents11.jpg`,
	minor_pentacles_12_knight: `${WIKI_BASE}/d/d5/Pents12.jpg`,
	minor_pentacles_13_queen: `${WIKI_BASE}/8/88/Pents13.jpg`,
	minor_pentacles_14_king: `${WIKI_BASE}/1/1c/Pents14.jpg`
};

export function getCardImageUrl(cardId: string): string | undefined {
	return cardImagePaths[cardId];
}
