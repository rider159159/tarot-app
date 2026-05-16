<script lang="ts">
	let {
		onclick,
		loading = false,
		disabled = false
	}: {
		onclick?: () => void;
		loading?: boolean;
		disabled?: boolean;
	} = $props();
</script>

<button type="submit" {onclick} disabled={disabled || loading} class="draw-btn">
	{loading ? '抽牌中...' : '抽牌'}
</button>

<style>
	/* 按鈕樣板：透明底 + 1px 外框 + pill 圓角 + monospace 字體 */
	.draw-btn {
		display: block;
		width: 100%;
		padding: var(--sp-4);
		font-family: var(--font-mono);
		font-size: var(--fs-body);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-1);
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		cursor: pointer;
		margin-bottom: var(--sp-6);
		transition: border-color var(--transition), color var(--transition);
	}

	.draw-btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.draw-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}
</style>
