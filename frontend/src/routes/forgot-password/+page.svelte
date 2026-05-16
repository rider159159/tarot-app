<script lang="ts">
	import { enhance } from '$app/forms';
	import type { ActionData } from './$types';
	import Seo from '$lib/components/Seo.svelte';

	let { form }: { form: ActionData } = $props();

	let submitting = $state(false);
</script>

<Seo title="忘記密碼" description="重設你的塔羅占卜帳號密碼。" noindex />

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

	.desc {
		font-size: var(--fs-sm);
		color: var(--c-text-3);
		margin: 0 0 var(--sp-4);
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

	.notice {
		color: var(--c-success);
		background: var(--c-success-bg);
		border: 1px solid var(--c-success);
		border-radius: var(--radius-0);
		padding: var(--sp-3);
		font-size: var(--fs-sm);
		margin: 0 0 var(--sp-4);
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
