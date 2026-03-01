<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();

	let submitting = $state(false);
	let password = $state('');

	let passwordTooShort = $derived(password.length > 0 && password.length < 8);
</script>

<svelte:head>
	<title>註冊 - 塔羅占卜</title>
</svelte:head>

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
			{#if form?.error}
				<p class="error">{form.error}</p>
			{/if}
			<button type="submit" disabled={submitting}>
				{submitting ? '註冊中...' : '註冊'}
			</button>
		</form>
		<p class="link">已有帳號？<a href="/login">登入</a></p>
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
