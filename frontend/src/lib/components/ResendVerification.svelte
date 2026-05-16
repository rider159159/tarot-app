<script lang="ts">
	import { onDestroy } from 'svelte';
	import { supabase } from '$lib/supabase';

	let { email }: { email: string } = $props();

	const COOLDOWN_SECONDS = 60;

	let cooldown = $state(0);
	let sending = $state(false);
	let message = $state('');
	let messageKind = $state<'notice' | 'error'>('notice');
	let timer: ReturnType<typeof setInterval> | undefined;

	function startCooldown() {
		cooldown = COOLDOWN_SECONDS;
		timer = setInterval(() => {
			cooldown -= 1;
			if (cooldown <= 0 && timer) {
				clearInterval(timer);
				timer = undefined;
			}
		}, 1000);
	}

	async function resend() {
		if (sending || cooldown > 0) return;
		sending = true;
		message = '';

		const { error } = await supabase.auth.resend({ type: 'signup', email });
		sending = false;

		if (error) {
			messageKind = 'error';
			message =
				error.status === 429
					? '操作過於頻繁，請稍候再試。'
					: '發送失敗，請稍後再試。';
			return;
		}

		messageKind = 'notice';
		message = '若此信箱尚未完成驗證，我們已重新寄出驗證信，請查收。';
		startCooldown();
	}

	onDestroy(() => {
		if (timer) clearInterval(timer);
	});
</script>

<div class="resend">
	<button type="button" onclick={resend} disabled={sending || cooldown > 0}>
		{#if sending}
			發送中...
		{:else if cooldown > 0}
			可在 {cooldown} 秒後重新發送
		{:else}
			沒收到信？重新發送驗證信
		{/if}
	</button>
	{#if message}
		<p class={messageKind}>{message}</p>
	{/if}
</div>

<style>
	.resend {
		margin-top: var(--sp-4);
		display: flex;
		flex-direction: column;
		gap: var(--sp-2);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	button {
		padding: var(--sp-2) var(--sp-4);
		background: transparent;
		color: var(--c-text-1);
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
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

	p {
		font-size: var(--fs-xs);
		margin: 0;
	}

	.notice {
		color: var(--c-success);
	}

	.error {
		color: var(--c-error);
	}
</style>
