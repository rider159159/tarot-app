import { randomUUID } from 'node:crypto';
import { env } from '$env/dynamic/private';
import { fail } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';
import { createServerApiClient, ApiError } from '$lib/server/api';
import { fetchSingleExport } from '$lib/server/export';
import type { ApiAnonymousDrawResponse, ApiReadingResponse } from '$lib/types';

const baseUrl = env.INTERNAL_API_URL || 'http://localhost:5098';

export const load: PageServerLoad = async ({ locals }) => {
	const { session } = await locals.safeGetSession();
	return { loggedIn: !!session };
};

export const actions: Actions = {
	draw: async ({ request, locals }) => {
		const data = await request.formData();
		const spreadType = data.get('spreadType') as string;
		const question = (data.get('question') as string | null) || null;

		const { session } = await locals.safeGetSession();

		// Logged in → persist via /api/readings (existing flow)
		if (session) {
			const api = createServerApiClient(session.access_token);
			try {
				const result = await api.post<ApiReadingResponse>('/api/readings', {
					spreadType,
					question
				});
				return { reading: result };
			} catch (err) {
				const status = err instanceof ApiError ? err.status : 400;
				const message = err instanceof Error ? err.message : '抽牌失敗，請稍後再試';
				return fail(status, { error: message });
			}
		}

		// Anonymous → call /api/tarot/draw without auth
		try {
			const res = await fetch(`${baseUrl}/api/tarot/draw`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ spreadType, question })
			});

			if (!res.ok) {
				const body = await res.json().catch(() => null);
				const msg = body?.error ?? (res.status === 429 ? '抽牌太頻繁，請稍候再試' : `抽牌失敗（${res.status}）`);
				return fail(res.status, { error: msg });
			}

			const drawn = (await res.json()) as ApiAnonymousDrawResponse;
			return {
				anonymousReading: drawn,
				clientToken: randomUUID()
			};
		} catch (err) {
			const message = err instanceof Error ? err.message : '抽牌失敗，請稍後再試';
			return fail(500, { error: message });
		}
	},

	importPending: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		if (!session) return fail(401, { importError: '請先登入' });

		const payloadStr = (await request.formData()).get('payload') as string | null;
		if (!payloadStr) return fail(400, { importError: '缺少資料' });

		type ImportPayload = {
			clientToken: string;
			spreadType: string;
			question: string | null;
			drawnAt: string;
			cards: Array<{ cardId: string; orientation: string; positionIndex: number }>;
		};

		let pending: ImportPayload;
		try {
			const raw = JSON.parse(payloadStr);
			pending = {
				clientToken: raw.clientToken,
				spreadType: raw.spreadType,
				question: raw.question ?? null,
				drawnAt: raw.drawnAt,
				cards: (raw.cards ?? []).map((c: { cardId: string; orientation: string; positionIndex: number }) => ({
					cardId: c.cardId,
					orientation: c.orientation,
					positionIndex: c.positionIndex
				}))
			};
		} catch {
			return fail(400, { importError: '資料格式錯誤' });
		}

		const api = createServerApiClient(session.access_token);
		try {
			const result = await api.post<ApiReadingResponse>('/api/readings/import', pending);
			return { importSuccess: true, importedReading: result };
		} catch (err) {
			const status = err instanceof ApiError ? err.status : 400;
			const message = err instanceof Error ? err.message : '保存失敗';
			return fail(status, { importError: message });
		}
	},

	exportSingle: async ({ request, locals }) => {
		const { session } = await locals.safeGetSession();
		const id = (await request.formData()).get('id')?.toString();
		if (!id) return fail(400, { exportError: '缺少 reading id' });
		return fetchSingleExport(session, id);
	}
};
