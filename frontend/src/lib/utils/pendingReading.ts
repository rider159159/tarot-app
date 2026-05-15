import type { ApiDrawnCard } from '$lib/types';

const STORAGE_KEY = 'pending-anonymous-reading';

export interface PendingReading {
	clientToken: string;
	spreadType: string; // dash-case (e.g. 'single', 'three-card-time')
	question: string | null;
	drawnAt: string; // ISO timestamp from server
	cards: ApiDrawnCard[];
}

export function savePending(reading: PendingReading): void {
	if (typeof localStorage === 'undefined') return;
	try {
		localStorage.setItem(STORAGE_KEY, JSON.stringify(reading));
	} catch {
		// Quota / private browsing — silently ignore.
	}
}

export function loadPending(): PendingReading | null {
	if (typeof localStorage === 'undefined') return null;
	try {
		const raw = localStorage.getItem(STORAGE_KEY);
		if (!raw) return null;
		const parsed = JSON.parse(raw) as PendingReading;
		if (!parsed?.clientToken || !Array.isArray(parsed.cards)) return null;
		return parsed;
	} catch {
		return null;
	}
}

export function clearPending(): void {
	if (typeof localStorage === 'undefined') return;
	try {
		localStorage.removeItem(STORAGE_KEY);
	} catch {
		// ignore
	}
}
