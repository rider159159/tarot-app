import { fail } from '@sveltejs/kit';
import type { Session } from '@supabase/supabase-js';
import { createServerApiClient, ApiError } from './api';
import type { ApiExportPayload, ApiExportBatch } from '$lib/types';

export async function fetchSingleExport(session: Session | null, id: string) {
	const api = createServerApiClient(session!.access_token);
	try {
		const payload = await api.get<ApiExportPayload>(`/api/readings/${id}/export`);
		return {
			exportSuccess: true as const,
			exportJson: JSON.stringify(payload, null, 2),
			exportedId: id
		};
	} catch (err) {
		const status = err instanceof ApiError ? err.status : 500;
		const message = err instanceof Error ? err.message : '匯出失敗，請稍後再試';
		console.error('[fetchSingleExport]', message);
		return fail(status, { exportError: '匯出失敗，請稍後再試' });
	}
}

export async function fetchBatchExport(session: Session | null) {
	const api = createServerApiClient(session!.access_token);
	try {
		const payload = await api.get<ApiExportBatch>('/api/readings/export');
		return {
			exportSuccess: true as const,
			exportJson: JSON.stringify(payload, null, 2),
			totalCount: payload.totalCount,
			exportedCount: payload.exportedCount,
			isTruncated: payload.isTruncated
		};
	} catch (err) {
		const status = err instanceof ApiError ? err.status : 500;
		const message = err instanceof Error ? err.message : '匯出失敗，請稍後再試';
		console.error('[fetchBatchExport]', message);
		return fail(status, { exportError: '匯出失敗，請稍後再試' });
	}
}
