<script lang="ts">
	import { signInWithGoogle } from '$lib/supabase';

	let { returnTo = '/' }: { returnTo?: string } = $props();

	let loading = $state(false);
	let error = $state('');

	async function handleClick() {
		loading = true;
		error = '';
		const { error: err } = await signInWithGoogle(returnTo);
		// On success the browser is redirected to Google, so we only land here on
		// failure (e.g. provider misconfigured, network error).
		if (err) {
			error = '無法開始 Google 登入，請稍後再試。';
			loading = false;
		}
	}
</script>

<div class="divider"><span>或</span></div>

<button type="button" class="google" onclick={handleClick} disabled={loading}>
	<svg viewBox="0 0 18 18" aria-hidden="true" width="18" height="18">
		<path
			fill="#4285F4"
			d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 0 1-1.8 2.72v2.26h2.92c1.7-1.57 2.68-3.88 2.68-6.62z"
		/>
		<path
			fill="#34A853"
			d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.92-2.26c-.8.54-1.84.86-3.04.86-2.34 0-4.32-1.58-5.02-3.7H.96v2.34A9 9 0 0 0 9 18z"
		/>
		<path
			fill="#FBBC05"
			d="M3.98 10.72a5.4 5.4 0 0 1 0-3.44V4.94H.96a9 9 0 0 0 0 8.12l3.02-2.34z"
		/>
		<path
			fill="#EA4335"
			d="M9 3.58c1.32 0 2.5.45 3.44 1.35l2.58-2.58A9 9 0 0 0 .96 4.94l3.02 2.34C4.68 5.16 6.66 3.58 9 3.58z"
		/>
	</svg>
	{loading ? '前往 Google...' : '使用 Google 登入'}
</button>
{#if error}
	<p class="error">{error}</p>
{/if}

<style>
	.divider {
		display: flex;
		align-items: center;
		gap: var(--sp-3);
		margin: var(--sp-4) 0;
		color: var(--c-text-3);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
	}

	.divider::before,
	.divider::after {
		content: '';
		flex: 1;
		height: 1px;
		background: var(--c-hairline);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill（沿用 login/register 的樣式） */
	.google {
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: var(--sp-2);
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

	.google:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.google:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.error {
		color: var(--c-error);
		font-size: var(--fs-sm);
		margin: var(--sp-3) 0 0;
		text-align: center;
	}
</style>
