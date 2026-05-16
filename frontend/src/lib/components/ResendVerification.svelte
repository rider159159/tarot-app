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
		margin-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	button {
		padding: 0.5rem 0.75rem;
		background: transparent;
		color: #4a3060;
		border: 1px solid #4a3060;
		border-radius: 6px;
		font-size: 0.875rem;
		cursor: pointer;
		font-family: inherit;
	}

	button:hover:not(:disabled) {
		background: #f1ecf5;
	}

	button:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}

	p {
		font-size: 0.8rem;
		margin: 0;
	}

	.notice {
		color: #2d6a2d;
	}

	.error {
		color: #a03030;
	}
</style>
