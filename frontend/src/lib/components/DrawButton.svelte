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
	.draw-btn {
		display: block;
		width: 100%;
		padding: 1rem;
		font-size: 1.125rem;
		font-weight: bold;
		color: white;
		background-color: #4a3060;
		border: none;
		border-radius: 8px;
		cursor: pointer;
		margin-bottom: 1.5rem;
		transition: background-color 0.2s;
	}

	.draw-btn:hover:not(:disabled) {
		background-color: #5d3d7a;
	}

	.draw-btn:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}
</style>
