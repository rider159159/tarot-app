import { env } from '$env/dynamic/private';

// Dev (Docker): http://backend:5098
// Prod (OCI self-host): http://backend:5098 (same Docker network, via docker-compose.prod.yml)
// Set via INTERNAL_API_URL env var (server-only, not PUBLIC_)
const baseUrl = env.INTERNAL_API_URL || 'http://localhost:5098';

export class ApiError extends Error {
	status: number;
	code?: string;

	constructor(status: number, message: string, code?: string) {
		super(message);
		this.name = 'ApiError';
		this.status = status;
		this.code = code;
	}
}

async function parseErrorResponse(res: Response, method: string, path: string): Promise<ApiError> {
	try {
		const body = await res.json();
		if (body?.error) {
			return new ApiError(res.status, body.error, body.code);
		}
	} catch {
		// Response body is not JSON or empty
	}
	return new ApiError(res.status, `${method} ${path} failed: ${res.status} ${res.statusText}`);
}

export function createServerApiClient(accessToken: string) {
	const headers = {
		'Content-Type': 'application/json',
		Authorization: `Bearer ${accessToken}`
	};

	return {
		async get<T>(path: string): Promise<T> {
			const res = await fetch(`${baseUrl}${path}`, { headers });
			if (!res.ok) {
				throw await parseErrorResponse(res, 'GET', path);
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
				throw await parseErrorResponse(res, 'POST', path);
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
				throw await parseErrorResponse(res, 'PUT', path);
			}
			return res.json();
		},

		async delete(path: string): Promise<void> {
			const res = await fetch(`${baseUrl}${path}`, {
				method: 'DELETE',
				headers
			});
			if (!res.ok) {
				throw await parseErrorResponse(res, 'DELETE', path);
			}
		}
	};
}
