import { fail } from '@sveltejs/kit';
import type { Actions } from './$types';
import { createServerApiClient, ApiError } from '$lib/server/api';
import type { ApiReadingResponse } from '$lib/types';

export const actions: Actions = {
	draw: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		const api = createServerApiClient(session!.access_token);

		const data = await request.formData();
		const spreadType = data.get('spreadType') as string;
		const question = data.get('question') as string | null;

		try {
			const result = await api.post<ApiReadingResponse>('/api/readings', {
				spreadType,
				question: question || null
			});
			return { reading: result };
		} catch (err) {
			const status = err instanceof ApiError ? err.status : 400;
			const message = err instanceof Error ? err.message : '抽牌失敗，請稍後再試';
			return fail(status, { error: message });
		}
	}
};
