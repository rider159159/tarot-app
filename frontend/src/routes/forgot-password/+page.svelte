<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();

	let submitting = $state(false);
</script>

<svelte:head>
	<title>忘記密碼 - 塔羅占卜</title>
</svelte:head>

<main>
	<div class="auth-card">
		<h1>忘記密碼</h1>
		{#if form?.sent}
			<p class="notice">若此信箱已註冊，我們已寄出密碼重設連結，請查收信件。</p>
			<p class="link"><a href="/login">返回登入</a></p>
		{:else}
			<p class="desc">輸入註冊時使用的電子郵件，我們會寄送重設密碼的連結給你。</p>
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
					電子郵件
					<input
						type="email"
						name="email"
						value={form?.email ?? ''}
						required
						disabled={submitting}
					/>
				</label>
				{#if form?.error}
					<p class="error">{form.error}</p>
				{/if}
				<button type="submit" disabled={submitting}>
					{submitting ? '送出中...' : '寄送重設連結'}
				</button>
			</form>
			<p class="link"><a href="/login">返回登入</a></p>
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

	.desc {
		font-size: 0.875rem;
		color: #666;
		margin: 0 0 1rem;
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

	.notice {
		color: #2d6a2d;
		background: #e8f5e8;
		border: 1px solid #a8d8a8;
		border-radius: 6px;
		padding: 0.75rem;
		font-size: 0.875rem;
		margin: 0 0 1rem;
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
