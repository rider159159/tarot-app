import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, locals: { safeGetSession } }) => {
	const returnTo = normalizeReturnTo(url.searchParams.get('returnTo'));
	const { session } = await safeGetSession();
	if (session) throw redirect(303, returnTo);
	return { returnTo };
};

export const actions: Actions = {
	default: async ({ request, url, locals: { supabase } }) => {
		const data = await request.formData();
		const email = data.get('email') as string;
		const password = data.get('password') as string;
		const displayName = data.get('display_name') as string;
		const returnTo = normalizeReturnTo((data.get('returnTo') as string) || null);

		if (!displayName || displayName.trim().length === 0) {
			return fail(400, { error: '請輸入顯示名稱', email, displayName, returnTo });
		}

		if (displayName.trim().length > 50) {
			return fail(400, { error: '顯示名稱不能超過 50 個字元', email, displayName, returnTo });
		}

		if (password.length < 8) {
			return fail(400, { error: '密碼至少需要 8 個字元', email, displayName, returnTo });
		}

		const emailRedirectTo = `${url.origin}/auth/callback?next=${encodeURIComponent(returnTo)}`;

		const { error } = await supabase.auth.signUp({
			email,
			password,
			options: {
				data: { display_name: displayName.trim() },
				emailRedirectTo
			}
		});

		if (error) {
			let message = error.message;
			if (
				error.message.toLowerCase().includes('already registered') ||
				error.message.toLowerCase().includes('already been registered')
			) {
				message = '此信箱已被註冊，請直接登入';
			}
			return fail(error.status || 400, { error: message, email, displayName, returnTo });
		}

		return { success: true, returnTo, email };
	}
};

function normalizeReturnTo(raw: string | null): string {
	if (!raw) return '/';
	if (!raw.startsWith('/')) return '/';
	if (raw.startsWith('//')) return '/';
	return raw;
}
