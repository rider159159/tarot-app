<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';
	import Seo from '$lib/components/Seo.svelte';

	let { form, data }: { form: ActionData; data: PageData } = $props();

	let submitting = $state(false);
	let password = $state('');
	let confirmPassword = $state('');

	let passwordTooShort = $derived(password.length > 0 && password.length < 8);
	let mismatch = $derived(confirmPassword.length > 0 && password !== confirmPassword);
</script>

<Seo title="重設密碼" description="設定新的塔羅占卜帳號密碼。" noindex />

<main>
	<div class="auth-card">
		<h1>重設密碼</h1>
		{#if data.ready}
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
				<label>
					新密碼
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
					確認新密碼
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
				{#if form?.error}
					<p class="error">{form.error}</p>
				{/if}
				<button type="submit" disabled={submitting || passwordTooShort || mismatch}>
					{submitting ? '更新中...' : '更新密碼'}
				</button>
			</form>
		{:else}
			<p class="error">
				重設連結無效或已過期。請重新申請一次密碼重設。
			</p>
			<p class="link"><a href="/forgot-password">重新申請重設密碼</a></p>
		{/if}
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
