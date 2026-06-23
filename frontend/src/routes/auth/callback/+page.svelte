<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { supabase } from '$lib/supabase';
	import type { PageData } from './$types';
	import Seo from '$lib/components/Seo.svelte';

	let { data }: { data: PageData } = $props();

	onMount(async () => {
		// OAuth failures get a neutral error; email-verification failures keep the
		// existing "already verified, just log in" notice (see comment below).
		const failureTarget =
			data.flow === 'oauth' ? '/login?error=oauth_failed' : '/login?notice=email_verified';

		// No code at all → nothing to exchange. Treat as a failed/old link.
		if (!data.code) {
			await goto(failureTarget, { replaceState: true });
			return;
		}

		const { error } = await supabase.auth.exchangeCodeForSession(data.code);

		if (error) {
			// For email verification: the email itself was already verified by
			// Supabase before redirecting here — only the auto-login (code exchange)
			// failed. This commonly happens when the link is opened in a different
			// browser/device than the one used to sign up, since the PKCE
			// code_verifier lives in that original browser's storage. So we send the
			// user to log in normally, NOT tell them verification failed.
			// For OAuth: the exchange failed for real, so we surface an error.
			await goto(failureTarget, { replaceState: true });
			return;
		}

		// Code exchange succeeded. Use a full-page navigation (not goto) so the
		// destination renders from a fresh server request that carries the
		// just-written session cookie. @supabase/ssr writes that cookie on the
		// onAuthStateChange tick — slightly AFTER exchangeCodeForSession resolves —
		// so a client-side goto can re-run the layout load before the cookie is
		// flushed and land "logged out" until a manual refresh.
		window.location.assign(data.next);
	});
</script>

<Seo title="驗證中" description="正在處理你的帳號驗證。" noindex />

<main>
	<p>正在完成信箱驗證，請稍候…</p>
</main>

<style>
	main {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 100vh;
		color: var(--c-text-3);
	}
</style>
