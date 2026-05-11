import { browser } from '$app/environment';

export type CopyStatus = 'idle' | 'copying' | 'done' | 'error';

export async function copyJsonToClipboard(jsonStr: string): Promise<boolean> {
	if (!browser) return false;
	try {
		await navigator.clipboard.writeText(jsonStr);
		return true;
	} catch (e) {
		console.error('clipboard write failed', e);
		return false;
	}
}

export function downloadJson(jsonStr: string, filename: string): void {
	if (!browser) return;
	const blob = new Blob([jsonStr], { type: 'application/json' });
	const url = URL.createObjectURL(blob);
	const a = document.createElement('a');
	a.href = url;
	a.download = filename;
	document.body.appendChild(a);
	a.click();
	document.body.removeChild(a);
	URL.revokeObjectURL(url);
}
