import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createServerApiClient, ApiError } from '$lib/server/api';
import type { ApiReadingsPage } from '$lib/types';

const PAGE_SIZE = 10;

export const load: PageServerLoad = async ({ locals, url }) => {
	const { session } = await locals.safeGetSession();
	const api = createServerApiClient(session!.access_token);

	const page = Number(url.searchParams.get('page') ?? '1');

	const data = await api.get<ApiReadingsPage>(
		`/api/readings?page=${page}&pageSize=${PAGE_SIZE}`
	);

	return {
		readings: data.items,
		totalCount: data.totalCount,
		currentPage: page,
		pageSize: PAGE_SIZE
	};
};

export const actions: Actions = {
	delete: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		const api = createServerApiClient(session!.access_token);

		const data = await request.formData();
		const id = data.get('id') as string;

		try {
			await api.delete(`/api/readings/${id}`);
			return { success: true };
		} catch (err) {
			const status = err instanceof ApiError ? err.status : 400;
			const message = err instanceof Error ? err.message : '刪除失敗，請稍後再試';
			return fail(status, { error: message });
		}
	}
};
