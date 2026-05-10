import { fail, redirect } from '@sveltejs/kit';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ locals: { safeGetSession } }) => {
	const { session } = await safeGetSession();
	if (session) throw redirect(303, '/');
	return {};
};

export const actions: Actions = {
	default: async ({ request, locals: { supabase } }) => {
		const data = await request.formData();
		const email = data.get('email') as string;
		const password = data.get('password') as string;

		if (!email || !password) {
			return fail(400, { error: '請填寫電子郵件和密碼', email });
		}

		const { error } = await supabase.auth.signInWithPassword({ email, password });
		if (error) {
			let message = error.message;
			if (error.message === 'Invalid login credentials') {
				message = '電子郵件或密碼錯誤';
			}
			return fail(error.status || 400, { error: message, email });
		}

		throw redirect(303, '/');
	}
};
