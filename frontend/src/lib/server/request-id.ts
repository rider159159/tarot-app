import { randomUUID } from 'node:crypto';

export const REQUEST_ID_HEADER = 'X-Request-ID';
const SAFE_REQUEST_ID = /^[A-Za-z0-9._-]+$/;
const MAX_REQUEST_ID_LENGTH = 128;

export function getOrCreateRequestId(candidate: string | null): string {
	if (
		candidate &&
		candidate.length <= MAX_REQUEST_ID_LENGTH &&
		SAFE_REQUEST_ID.test(candidate)
	) {
		return candidate;
	}

	return randomUUID().replaceAll('-', '');
}
