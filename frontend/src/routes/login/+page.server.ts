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

		const { error } = await supabase.auth.signInWithPassword({ email, password });
		if (error) return fail(error.status || 400, { error: error.message, email });

		throw redirect(303, '/');
	}
};
