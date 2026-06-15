<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';
	import ResendVerification from '$lib/components/ResendVerification.svelte';
	import GoogleSignInButton from '$lib/components/GoogleSignInButton.svelte';
	import Seo from '$lib/components/Seo.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let returnTo = $derived(form?.returnTo ?? data.returnTo ?? '/');
	let submitting = $state(false);
	let password = $state('');
	let confirmPassword = $state('');

	let passwordTooShort = $derived(password.length > 0 && password.length < 8);
	let mismatch = $derived(confirmPassword.length > 0 && password !== confirmPassword);
</script>

<Seo
	title="註冊"
	description="免費註冊塔羅占卜帳號，開始記錄並管理你的塔羅牌占卜歷程。"
/>

<main>
	<div class="auth-card">
		<h1>註冊</h1>
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
				顯示名稱
				<input
					type="text"
					name="display_name"
					value={form?.displayName ?? ''}
					required
					disabled={submitting}
				/>
			</label>
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
				<input
					type="password"
					name="password"
					bind:value={password}
					required
					minlength="8"
					disabled={submitting}
				/>
				{#if passwordTooShort}
					<span class="hint">密碼至少需要 8 個字元</span>
				{/if}
			</label>
			<label>
				確認密碼
				<input
					type="password"
					name="confirm_password"
					bind:value={confirmPassword}
					required
					minlength="8"
					disabled={submitting}
				/>
				{#if mismatch}
					<span class="hint">兩次輸入的密碼不一致</span>
				{/if}
			</label>
			{#if form?.success}
				<p class="success">註冊成功！請查收驗證信，點擊信中連結完成註冊。</p>
			{:else if form?.error}
				<p class="error">{form.error}</p>
			{/if}
			<button
				type="submit"
				disabled={submitting || !!form?.success || passwordTooShort || mismatch}
			>
				{submitting ? '註冊中...' : '註冊'}
			</button>
		</form>
		{#if form?.success && form?.email}
			<ResendVerification email={form.email} />
		{/if}
		<GoogleSignInButton {returnTo} />
		<p class="link">已有帳號？<a href={`/login?returnTo=${encodeURIComponent(returnTo)}`}>登入</a></p>
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

	.hint {
		color: var(--c-warning);
		font-size: var(--fs-xs);
	}

	.success {
		color: var(--c-success);
		background: var(--c-success-bg);
		border: 1px solid var(--c-success);
		border-radius: var(--radius-0);
		padding: var(--sp-3);
		font-size: var(--fs-sm);
		margin: 0;
	}

	.error {
		color: var(--c-error);
		font-size: var(--fs-sm);
		margin: 0;
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
