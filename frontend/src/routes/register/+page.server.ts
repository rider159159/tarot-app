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
		const displayName = data.get('display_name') as string;

		if (password.length < 8) {
			return fail(400, { error: '密碼至少需要 8 個字元', email, displayName });
		}

		const { error } = await supabase.auth.signUp({
			email,
			password,
			options: { data: { display_name: displayName } }
		});

		if (error) return fail(error.status || 400, { error: error.message, email, displayName });

		throw redirect(303, '/');
	}
};
