<script lang="ts">
	import { onMount } from 'svelte';
	import { goto, invalidateAll } from '$app/navigation';
	import { supabase } from '$lib/supabase';
	import type { PageData } from './$types';
	import Seo from '$lib/components/Seo.svelte';

	let { data }: { data: PageData } = $props();

	onMount(async () => {
		// No code at all → nothing to exchange. Treat as a failed/old link.
		if (!data.code) {
			await goto('/login?notice=email_verified', { replaceState: true });
			return;
		}

		const { error } = await supabase.auth.exchangeCodeForSession(data.code);

		if (error) {
			// The email itself was already verified by Supabase before redirecting
			// here — only the auto-login (code exchange) failed. This commonly
			// happens when the link is opened in a different browser/device than
			// the one used to sign up, since the PKCE code_verifier lives in that
			// original browser's storage. So we send the user to log in normally,
			// NOT tell them verification failed.
			await goto('/login?notice=email_verified', { replaceState: true });
			return;
		}

		// Code exchange succeeded — the user is now logged in. Refresh server
		// load data so hooks.server.ts sees the new session cookie.
		await invalidateAll();
		await goto(data.next, { replaceState: true });
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
