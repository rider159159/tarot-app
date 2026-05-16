import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ url, locals: { supabase, safeGetSession } }) => {
	const code = url.searchParams.get('code');

	if (code) {
		const { error } = await supabase.auth.exchangeCodeForSession(code);
		// On success the recovery session is now stored in cookies. Redirect to a
		// clean URL so a form re-submit doesn't try to consume the code twice.
		if (error) return { ready: false };
		throw redirect(303, '/auth/reset-password');
	}

	const { session } = await safeGetSession();
	return { ready: !!session };
};

export const actions: Actions = {
	default: async ({ request, locals: { supabase, safeGetSession } }) => {
		const { session } = await safeGetSession();
		if (!session) {
			return fail(401, { error: '重設連結已失效，請重新申請重設密碼。' });
		}

		const data = await request.formData();
		const password = data.get('password') as string;
		const confirmPassword = data.get('confirm_password') as string;

		if (!password || password.length < 8) {
			return fail(400, { error: '密碼至少需要 8 個字元' });
		}
		if (password !== confirmPassword) {
			return fail(400, { error: '兩次輸入的密碼不一致' });
		}

		const { error } = await supabase.auth.updateUser({ password });
		if (error) {
			return fail(error.status || 400, { error: '密碼更新失敗，請稍後再試。' });
		}

		// Sign out so the user re-authenticates with the new password.
		await supabase.auth.signOut();
		throw redirect(303, '/login?reset=success');
	}
};
