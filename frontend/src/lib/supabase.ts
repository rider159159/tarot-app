import { createBrowserClient } from '@supabase/ssr';
import { parse, serialize } from 'cookie';
import type { SupportedStorage } from '@supabase/supabase-js';
import { PUBLIC_SUPABASE_URL, PUBLIC_SUPABASE_ANON_KEY } from '$env/static/public';

// With `encode: 'tokens-only'` the user object is kept in `userStorage` instead
// of the cookie. @supabase/ssr defaults that to `window.localStorage`, which it
// touches at construction time — and this module is imported into SSR'd code,
// where `window` is undefined and the client would throw. Supplying an explicit
// SSR-safe storage avoids that: the browser uses real localStorage; on the
// server (where this client is never the source of truth) it is a no-op memory
// store.
const memory = new Map<string, string>();
const userStorage: SupportedStorage =
	typeof window !== 'undefined'
		? window.localStorage
		: {
				getItem: (key) => memory.get(key) ?? null,
				setItem: (key, value) => void memory.set(key, value),
				removeItem: (key) => void memory.delete(key)
			};

// `encode: 'tokens-only'` keeps the ~1KB user object OUT of the auth cookie so
// it stays small enough to survive the reverse proxy's response-header buffer
// when the session is refreshed. This MUST match the 'tokens-only' encoding in
// hooks.server.ts — if the browser and server clients disagree, the cookie
// format written by one is read inconsistently by the other (stale chunks →
// random logouts). Supplying `encode` requires also supplying getAll/setAll;
// the two below reproduce @supabase/ssr's own default document.cookie handling.
export const supabase = createBrowserClient(PUBLIC_SUPABASE_URL, PUBLIC_SUPABASE_ANON_KEY, {
	auth: { userStorage },
	cookies: {
		encode: 'tokens-only',
		getAll() {
			if (typeof document === 'undefined') return [];
			const parsed = parse(document.cookie);
			return Object.keys(parsed).map((name) => ({ name, value: parsed[name] ?? '' }));
		},
		setAll(cookiesToSet) {
			if (typeof document === 'undefined') return;
			for (const { name, value, options } of cookiesToSet) {
				document.cookie = serialize(name, value, options);
			}
		}
	}
});

// Start the Google OAuth flow. redirectTo points back at our existing PKCE
// callback (/auth/callback), which exchanges the code for a session. We tag it
// with flow=oauth so the callback can show an OAuth-specific error message
// instead of the email-verification one when the exchange fails. next carries
// the post-login destination, mirroring the email/password returnTo behaviour.
export function signInWithGoogle(returnTo: string = '/') {
	const redirectTo = `${window.location.origin}/auth/callback?next=${encodeURIComponent(
		returnTo
	)}&flow=oauth`;
	return supabase.auth.signInWithOAuth({
		provider: 'google',
		options: { redirectTo }
	});
}
