import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const load: PageServerLoad = async ({ locals: { safeGetSession } }) => {
	const { session } = await safeGetSession();
	if (session) throw redirect(303, '/');
	return {};
};

export const actions: Actions = {
	default: async ({ request, url, locals: { supabase } }) => {
		const data = await request.formData();
		const email = ((data.get('email') as string) ?? '').trim();

		if (!email || !EMAIL_RE.test(email)) {
			return fail(400, { error: '請輸入有效的電子郵件', email });
		}

		// Neutral response regardless of whether the email exists — avoids account enumeration.
		await supabase.auth.resetPasswordForEmail(email, {
			redirectTo: `${url.origin}/auth/reset-password`
		});

		return { sent: true };
	}
};
