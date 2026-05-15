import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, locals: { safeGetSession } }) => {
	const returnTo = normalizeReturnTo(url.searchParams.get('returnTo'));
	const { session } = await safeGetSession();
	if (session) throw redirect(303, returnTo);
	return { returnTo };
};

export const actions: Actions = {
	default: async ({ request, locals: { supabase } }) => {
		const data = await request.formData();
		const email = data.get('email') as string;
		const password = data.get('password') as string;
		const returnTo = normalizeReturnTo((data.get('returnTo') as string) || null);

		if (!email || !password) {
			return fail(400, { error: '請填寫電子郵件和密碼', email, returnTo });
		}

		const { error } = await supabase.auth.signInWithPassword({ email, password });
		if (error) {
			let message = error.message;
			if (error.message === 'Invalid login credentials') {
				message = '電子郵件或密碼錯誤';
			}
			return fail(error.status || 400, { error: message, email, returnTo });
		}

		throw redirect(303, returnTo);
	}
};

// Only allow same-origin paths. Anything weird (absolute URL, protocol, '//') falls back to '/'.
function normalizeReturnTo(raw: string | null): string {
	if (!raw) return '/';
	if (!raw.startsWith('/')) return '/';
	if (raw.startsWith('//')) return '/';
	return raw;
}
