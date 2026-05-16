<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';
	import ResendVerification from '$lib/components/ResendVerification.svelte';
	import Seo from '$lib/components/Seo.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	// form.returnTo (after fail) wins, falls back to load's data.returnTo, then '/'.
	let returnTo = $derived(form?.returnTo ?? data.returnTo ?? '/');
	let submitting = $state(false);
</script>

<Seo
	title="登入"
	description="登入塔羅占卜，保存你的占卜歷史並隨時回顧過往牌陣。"
	noindex
/>

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
		padding: var(--sp-4);
	}

	.auth-card {
		width: 100%;
		max-width: 400px;
	}

	h1 {
		text-align: center;
		font-size: var(--fs-h1);
		letter-spacing: var(--ls-heading);
		margin-bottom: var(--sp-8);
	}

	form {
		display: flex;
		flex-direction: column;
		gap: var(--sp-4);
	}

	label {
		display: flex;
		flex-direction: column;
		gap: var(--sp-1);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
	}

	input {
		padding: var(--sp-3);
		font-family: var(--font-serif);
		font-size: var(--fs-body);
	}

	input:disabled {
		opacity: 0.5;
	}

	.error {
		color: var(--c-error);
		font-size: var(--fs-sm);
		margin: 0;
	}

	.banner {
		font-size: var(--fs-sm);
		border-radius: var(--radius-0);
		padding: var(--sp-3);
		margin: 0 0 var(--sp-4);
	}

	.banner-error {
		color: var(--c-error);
		background: var(--c-error-bg);
		border: 1px solid var(--c-error);
	}

	.banner-notice {
		color: var(--c-success);
		background: var(--c-success-bg);
		border: 1px solid var(--c-success);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	button {
		padding: var(--sp-3);
		background: transparent;
		color: var(--c-text-1);
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		font-family: var(--font-mono);
		font-size: var(--fs-body);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	button:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	button:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.link {
		text-align: center;
		margin-top: var(--sp-4);
		font-size: var(--fs-sm);
		color: var(--c-text-3);
	}

	.link a {
		color: var(--c-link);
		text-decoration: none;
	}

	.link a:hover {
		color: var(--c-link-hover);
	}
</style>
