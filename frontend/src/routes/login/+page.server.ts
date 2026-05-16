import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

const LOAD_MESSAGES: Record<string, { kind: 'error' | 'notice'; text: string }> = {
	email_verification_failed: {
		kind: 'error',
		text: '信箱驗證失敗，連結可能已過期或無效。請重新發送驗證信。'
	},
	email_verified: {
		kind: 'notice',
		text: '信箱已完成驗證，請使用您的帳號密碼登入。'
	},
	reset_success: {
		kind: 'notice',
		text: '密碼已更新，請使用新密碼重新登入。'
	}
};

export const load: PageServerLoad = async ({ url, locals: { safeGetSession } }) => {
	const returnTo = normalizeReturnTo(url.searchParams.get('returnTo'));
	const { session } = await safeGetSession();
	if (session) throw redirect(303, returnTo);

	const errorParam = url.searchParams.get('error');
	const noticeParam = url.searchParams.get('notice');
	const resetParam = url.searchParams.get('reset');
	const key = resetParam === 'success' ? 'reset_success' : noticeParam || errorParam;

	const matched = key ? LOAD_MESSAGES[key] : undefined;
	return {
		returnTo,
		loadError: matched?.kind === 'error' ? matched.text : null,
		loadNotice: matched?.kind === 'notice' ? matched.text : null
	};
};

export const actions: Actions = {
	default: async ({ request, locals: { supabase } }) => {
		const data = await request.formData();
		const email = data.get('email') as string;
		const password = data.get('password') as string;
		const returnTo = normalizeReturnTo((data.get('returnTo') as string) || null);

		if (!email || !password) {
			return fail(400, { error: '請填寫電子郵件和密碼', email, returnTo, needsVerification: false });
		}

		const { error } = await supabase.auth.signInWithPassword({ email, password });
		if (error) {
			let message = error.message;
			let needsVerification = false;
			if (error.message === 'Invalid login credentials') {
				message = '電子郵件或密碼錯誤';
			} else if (error.message === 'Email not confirmed') {
				message = '此信箱尚未完成驗證，請點擊驗證信中的連結，或重新發送驗證信。';
				needsVerification = true;
			}
			return fail(error.status || 400, { error: message, email, returnTo, needsVerification });
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
