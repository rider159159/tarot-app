<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData, PageData } from './$types';

	let { form, data }: { form: ActionData; data: PageData } = $props();

	let submitting = $state(false);
	let password = $state('');
	let confirmPassword = $state('');

	let passwordTooShort = $derived(password.length > 0 && password.length < 8);
	let mismatch = $derived(confirmPassword.length > 0 && password !== confirmPassword);
</script>

<svelte:head>
	<title>重設密碼 - 塔羅占卜</title>
</svelte:head>

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

	.hint {
		color: #b08000;
		font-size: 0.8rem;
	}

	.error {
		color: #a03030;
		font-size: 0.875rem;
		margin: 0;
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
