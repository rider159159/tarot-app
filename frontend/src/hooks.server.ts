import { createServerClient } from '@supabase/ssr';
import { redirect, type Handle } from '@sveltejs/kit';
import { PUBLIC_SUPABASE_URL, PUBLIC_SUPABASE_ANON_KEY } from '$env/static/public';

// '/' is now public so anonymous users can draw cards.
// /history, /profile remain auth-gated by default (not in this list).
const PUBLIC_PATHS = [
	'/',
	'/login',
	'/register',
	'/auth/callback',
	'/forgot-password',
	'/auth/reset-password'
];

export const handle: Handle = async ({ event, resolve }) => {
	event.locals.supabase = createServerClient(PUBLIC_SUPABASE_URL, PUBLIC_SUPABASE_ANON_KEY, {
		cookies: {
			// Keep the ~1KB user object OUT of the auth cookie (it lives in a
			// separate store instead), so the refreshed Set-Cookie header stays
			// small. Without this, on token refresh the chunked session cookie
			// bloats the response headers past the reverse proxy's buffer and
			// triggers a 502. The browser client (lib/supabase.ts) MUST use the
			// same 'tokens-only' encoding, or the two disagree on cookie format.
			encode: 'tokens-only',
			getAll: () => event.cookies.getAll(),
			setAll: (cookiesToSet) => {
				// @supabase/ssr defaults auth cookies to Secure, which browsers drop
				// over plain HTTP. While we serve the app over http:// (IP, no domain
				// yet), force secure=false so the session cookie actually sticks.
				// Once a domain + HTTPS is in place, event.url.protocol becomes
				// 'https:' and the cookie is Secure again automatically.
				const secure = event.url.protocol === 'https:';
				cookiesToSet.forEach(({ name, value, options }) =>
					event.cookies.set(name, value, { path: '/', ...options, secure })
				);
			}
		}
	});

	event.locals.safeGetSession = async () => {
		const {
			data: { session }
		} = await event.locals.supabase.auth.getSession();
		if (!session) return { session: null, user: null };

		const {
			data: { user },
			error
		} = await event.locals.supabase.auth.getUser();
		if (error) return { session: null, user: null };

		return { session, user };
	};

	const { session } = await event.locals.safeGetSession();
	const path = event.url.pathname;
	// Use exact match for '/' to avoid accidentally allowing '/anything'.
	const isPublic =
		path === '/' || PUBLIC_PATHS.some((p) => p !== '/' && path.startsWith(p));

	if (!session && !isPublic) {
		throw redirect(303, '/login');
	}
	if (session && (path === '/login' || path === '/register')) {
		throw redirect(303, '/');
	}

	return resolve(event, {
		filterSerializedResponseHeaders(name) {
			return name === 'content-range' || name === 'x-supabase-api-version';
		}
	});
};
