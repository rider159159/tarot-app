import { env } from '$env/dynamic/private';

// Dev (Docker): http://backend:5098
// Prod (Zeabur): http://<service-name>.zeabur.internal:8080
// Set via INTERNAL_API_URL env var (server-only, not PUBLIC_)
const baseUrl = env.INTERNAL_API_URL || 'http://localhost:5098';

export function createServerApiClient(accessToken: string) {
	const headers = {
		'Content-Type': 'application/json',
		Authorization: `Bearer ${accessToken}`
	};

	return {
		async get<T>(path: string): Promise<T> {
			const res = await fetch(`${baseUrl}${path}`, { headers });
			if (!res.ok) {
				throw new Error(`GET ${path} failed: ${res.status} ${res.statusText}`);
			}
			return res.json();
		},

		async post<T>(path: string, body: unknown): Promise<T> {
			const res = await fetch(`${baseUrl}${path}`, {
				method: 'POST',
				headers,
				body: JSON.stringify(body)
			});
			if (!res.ok) {
				throw new Error(`POST ${path} failed: ${res.status} ${res.statusText}`);
			}
			return res.json();
		},

		async put<T>(path: string, body: unknown): Promise<T> {
			const res = await fetch(`${baseUrl}${path}`, {
				method: 'PUT',
				headers,
				body: JSON.stringify(body)
			});
			if (!res.ok) {
				throw new Error(`PUT ${path} failed: ${res.status} ${res.statusText}`);
			}
			return res.json();
		},

		async delete(path: string): Promise<void> {
			const res = await fetch(`${baseUrl}${path}`, {
				method: 'DELETE',
				headers
			});
			if (!res.ok) {
				throw new Error(`DELETE ${path} failed: ${res.status} ${res.statusText}`);
			}
		}
	};
}
