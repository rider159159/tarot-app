import type { ReadingResult, DrawnCardRow } from '$lib/types';
import { supabase } from '$lib/supabase';

export async function saveReading(result: ReadingResult): Promise<string | null> {
	const {
		data: { user }
	} = await supabase.auth.getUser();

	if (!user) {
		console.warn('User not authenticated; reading will not be saved.');
		return null;
	}

	const cardsPayload: DrawnCardRow[] = result.cards.map((dc) => ({
		card_id: dc.card.id,
		orientation: dc.orientation,
		position_index: dc.position.index
	}));

	const { data, error } = await supabase
		.from('readings')
		.insert({
			user_id: user.id,
			spread_type: result.spreadType,
			question: result.question,
			cards: cardsPayload,
			created_at: result.createdAt
		})
		.select('id')
		.single();

	if (error) {
		console.error('Failed to save reading:', error);
		return null;
	}

	return data.id;
}
