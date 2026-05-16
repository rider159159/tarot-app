import type { PageServerLoad } from './$types';

// The actual code exchange happens in the browser (+page.svelte) because the
// PKCE code_verifier is stored in browser storage by createBrowserClient at
// signUp() time. A server-side exchange can't read it, so we only pass through
// the query params here.
export const load: PageServerLoad = async ({ url }) => {
	return {
		code: url.searchParams.get('code'),
		next: normalizeNext(url.searchParams.get('next'))
	};
};

// Only allow same-origin paths to avoid open-redirect.
function normalizeNext(raw: string | null): string {
	if (!raw) return '/';
	if (!raw.startsWith('/')) return '/';
	if (raw.startsWith('//')) return '/';
	return raw;
}
