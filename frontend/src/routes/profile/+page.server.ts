import { fail, error } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createServerApiClient, ApiError } from '$lib/server/api';
import { fetchSingleExport } from '$lib/server/export';
import type {
	ApiProfileResponse,
	ApiReadingStatsResponse,
	ApiWeeklyFortuneResponse,
	ApiReadingResponse
} from '$lib/types';

// Empty stats used when the stats endpoint fails — the page degrades to
// "no stats yet" rather than 500ing on a non-critical request.
const EMPTY_STATS: ApiReadingStatsResponse = {
	totalCount: 0,
	topCards: [],
	spreadUsage: [],
	lastReadingAt: null
};

export const load: PageServerLoad = async ({ locals }) => {
	const { session } = await locals.safeGetSession();
	const api = createServerApiClient(session!.access_token);

	// allSettled (not Promise.all) so a single failing request doesn't take
	// down the whole page. profile is critical — if it fails we still 500;
	// stats and weeklyFortune degrade gracefully.
	const [profileResult, statsResult, weeklyResult] = await Promise.allSettled([
		api.get<ApiProfileResponse>('/api/profile'),
		api.get<ApiReadingStatsResponse>('/api/readings/stats'),
		api.get<ApiWeeklyFortuneResponse>('/api/readings/weekly-fortune')
	]);

	if (profileResult.status === 'rejected') {
		throw error(502, '無法載入使用者資料，請稍後再試');
	}

	const stats = statsResult.status === 'fulfilled' ? statsResult.value : EMPTY_STATS;
	const weeklyFortune: ApiWeeklyFortuneResponse =
		weeklyResult.status === 'fulfilled'
			? weeklyResult.value
			: { reading: null, canDraw: true };

	return { profile: profileResult.value, stats, weeklyFortune };
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
	},

	exportSingle: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		const id = (await request.formData()).get('id')?.toString();
		if (!id) return fail(400, { exportError: '缺少 reading id' });
		return fetchSingleExport(session, id);
	}
};
