<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';
	import ResendVerification from '$lib/components/ResendVerification.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	// form.returnTo (after fail) wins, falls back to load's data.returnTo, then '/'.
	let returnTo = $derived(form?.returnTo ?? data.returnTo ?? '/');
	let submitting = $state(false);
</script>

<svelte:head>
	<title>登入 - 塔羅占卜</title>
</svelte:head>

<main>
	<div class="auth-card">
		<h1>登入</h1>
		{#if data.loadNotice}
			<p class="banner banner-notice">{data.loadNotice}</p>
		{/if}
		{#if data.loadError}
			<p class="banner banner-error">{data.loadError}</p>
		{/if}
		<form
			method="POST"
			use:enhance={() => {
				submitting = true;
				return async ({ update }) => {
					submitting = false;
					await update();
				};
			}}
		>
			<input type="hidden" name="returnTo" value={returnTo} />
			<label>
				電子郵件
				<input
					type="email"
					name="email"
					value={form?.email ?? ''}
					required
					disabled={submitting}
				/>
			</label>
			<label>
				密碼
				<input type="password" name="password" required disabled={submitting} />
			</label>
			{#if form?.error}
				<p class="error">{form.error}</p>
			{/if}
			<button type="submit" disabled={submitting}>
				{submitting ? '登入中...' : '登入'}
			</button>
		</form>
		{#if form?.needsVerification && form?.email}
			<ResendVerification email={form.email} />
		{/if}
		<p class="link"><a href="/forgot-password">忘記密碼？</a></p>
		<p class="link">還沒有帳號？<a href={`/register?returnTo=${encodeURIComponent(returnTo)}`}>註冊</a></p>
	</div>
</main>

<style>
	main {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 100vh;
		font-family: system-ui, -apple-system, sans-serif;
		padding: 1rem;
	}

	.auth-card {
		width: 100%;
		max-width: 400px;
	}

	h1 {
		text-align: center;
		color: #333;
		margin-bottom: 1.5rem;
	}

	form {
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	label {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		font-size: 0.875rem;
		color: #555;
	}

	input {
		padding: 0.625rem 0.75rem;
		border: 1px solid #ccc;
		border-radius: 6px;
		font-size: 1rem;
		font-family: inherit;
	}

	input:focus {
		outline: none;
		border-color: #4a3060;
		box-shadow: 0 0 0 2px rgba(74, 48, 96, 0.2);
	}

	input:disabled {
		opacity: 0.6;
	}

	.error {
		color: #a03030;
		font-size: 0.875rem;
		margin: 0;
	}

	.banner {
		font-size: 0.875rem;
		border-radius: 6px;
		padding: 0.75rem;
		margin: 0 0 1rem;
	}

	.banner-error {
		color: #a03030;
		background: #fbeaea;
		border: 1px solid #e0a8a8;
	}

	.banner-notice {
		color: #2d6a2d;
		background: #e8f5e8;
		border: 1px solid #a8d8a8;
	}

	button {
		padding: 0.75rem;
		background: #4a3060;
		color: white;
		border: none;
		border-radius: 6px;
		font-size: 1rem;
		cursor: pointer;
		font-family: inherit;
	}

	button:hover:not(:disabled) {
		background: #5a3d73;
	}

	button:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}

	.link {
		text-align: center;
		margin-top: 1rem;
		font-size: 0.875rem;
		color: #666;
	}

	.link a {
		color: #4a3060;
		text-decoration: none;
		font-weight: 500;
	}

	.link a:hover {
		text-decoration: underline;
	}
</style>
