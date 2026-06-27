import type { PageServerLoad } from './$types';
import { redirect } from '@sveltejs/kit';

export const load: PageServerLoad = async ({ url, locals: { supabase } }) => {
	const code = url.searchParams.get('code');
	const next = normalizeNext(url.searchParams.get('next'));
	const flow = url.searchParams.get('flow');

	if (code) {
		const { error } = await supabase.auth.exchangeCodeForSession(code);
		if (!error) {
			throw redirect(303, next);
		}
	}

	const failureTarget =
		flow === 'oauth' ? '/login?error=oauth_failed' : '/login?notice=email_verified';
	throw redirect(303, failureTarget);
};

function normalizeNext(raw: string | null): string {
	if (!raw) return '/';
	if (!raw.startsWith('/')) return '/';
	if (raw.startsWith('//')) return '/';
	return raw;
}
