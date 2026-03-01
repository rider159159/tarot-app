import { PUBLIC_API_BASE_URL } from '$env/static/public';
import { supabase } from '$lib/supabase';

const baseUrl = PUBLIC_API_BASE_URL || 'http://localhost:5098';

async function getAuthHeaders(): Promise<Record<string, string>> {
	const headers: Record<string, string> = { 'Content-Type': 'application/json' };

	if (supabase) {
		const { data } = await supabase.auth.getSession();
		if (data.session?.access_token) {
			headers['Authorization'] = `Bearer ${data.session.access_token}`;
		}
	}

	return headers;
}

export async function apiGet<T>(path: string): Promise<T> {
	const headers = await getAuthHeaders();
	const res = await fetch(`${baseUrl}${path}`, { headers });

	if (!res.ok) {
		throw new Error(`GET ${path} failed: ${res.status} ${res.statusText}`);
	}

	return res.json();
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
	const headers = await getAuthHeaders();
	const res = await fetch(`${baseUrl}${path}`, {
		method: 'POST',
		headers,
		body: JSON.stringify(body)
	});

	if (!res.ok) {
		throw new Error(`POST ${path} failed: ${res.status} ${res.statusText}`);
	}

	return res.json();
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
	const headers = await getAuthHeaders();
	const res = await fetch(`${baseUrl}${path}`, {
		method: 'PUT',
		headers,
		body: JSON.stringify(body)
	});

	if (!res.ok) {
		throw new Error(`PUT ${path} failed: ${res.status} ${res.statusText}`);
	}

	return res.json();
}

export async function apiDelete(path: string): Promise<void> {
	const headers = await getAuthHeaders();
	const res = await fetch(`${baseUrl}${path}`, {
		method: 'DELETE',
		headers
	});

	if (!res.ok) {
		throw new Error(`DELETE ${path} failed: ${res.status} ${res.statusText}`);
	}
}
