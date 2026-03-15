import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createServerApiClient, ApiError } from '$lib/server/api';
import type {
	ApiProfileResponse,
	ApiReadingStatsResponse,
	ApiWeeklyFortuneResponse,
	ApiReadingResponse
} from '$lib/types';

export const load: PageServerLoad = async ({ locals }) => {
	const { session } = await locals.safeGetSession();
	const api = createServerApiClient(session!.access_token);

	const [profile, stats, weeklyFortune] = await Promise.all([
		api.get<ApiProfileResponse>('/api/profile'),
		api.get<ApiReadingStatsResponse>('/api/readings/stats'),
		api.get<ApiWeeklyFortuneResponse>('/api/readings/weekly-fortune')
			.catch((): ApiWeeklyFortuneResponse => ({ reading: null, canDraw: true }))
	]);

	return { profile, stats, weeklyFortune };
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
			const status = err instanceof ApiError ? err.status : 400;
			const message = err instanceof Error ? err.message : '儲存失敗，請稍後再試';
			return fail(status, { error: message });
		}
	},

	drawWeekly: async ({ locals }) => {
		const { session } = await locals.safeGetSession();
		const api = createServerApiClient(session!.access_token);

		try {
			const reading = await api.post<ApiReadingResponse>('/api/readings/weekly-fortune', {});
			return { weeklyReading: reading };
		} catch (err) {
			const status = err instanceof ApiError ? err.status : 400;
			const message = err instanceof Error ? err.message : '抽牌失敗，請稍後再試';
			return fail(status, { weeklyError: message });
		}
	}
};
