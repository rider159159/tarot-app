import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createServerApiClient } from '$lib/server/api';
import type { ApiProfileResponse, ApiReadingStatsResponse } from '$lib/types';

export const load: PageServerLoad = async ({ locals }) => {
	const { session } = await locals.safeGetSession();
	const api = createServerApiClient(session!.access_token);

	const [profile, stats] = await Promise.all([
		api.get<ApiProfileResponse>('/api/profile'),
		api.get<ApiReadingStatsResponse>('/api/readings/stats')
	]);

	return { profile, stats };
};

export const actions: Actions = {
	updateName: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		const api = createServerApiClient(session!.access_token);

		const data = await request.formData();
		const displayName = data.get('displayName') as string;

		try {
			const updated = await api.put<ApiProfileResponse>('/api/profile', { displayName });
			return { success: true, profile: updated };
		} catch (err) {
			return fail(400, { error: err instanceof Error ? err.message : '儲存失敗，請稍後再試' });
		}
	}
};
