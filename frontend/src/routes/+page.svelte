<script lang="ts">
	import type { SpreadType, ReadingResult, ApiReadingResponse } from '$lib/types';
	import { apiPost } from '$lib/api';
	import { mapApiResponse } from '$lib/utils/reading';
	import SpreadSelector from '$lib/components/SpreadSelector.svelte';
	import QuestionInput from '$lib/components/QuestionInput.svelte';
	import DrawButton from '$lib/components/DrawButton.svelte';
	import ReadingDisplay from '$lib/components/ReadingDisplay.svelte';

	let selectedSpread: SpreadType = $state('single');
	let question: string = $state('');
	let loading: boolean = $state(false);
	let reading: ReadingResult | null = $state(null);
	let error: string | null = $state(null);

	async function handleDraw() {
		loading = true;
		reading = null;
		error = null;

		try {
			const res = await apiPost<ApiReadingResponse>('/api/readings', {
				spreadType: selectedSpread === 'three-card' ? 'ThreeCard' : selectedSpread === 'celtic-cross' ? 'CelticCross' : 'Single',
				question: question || null
			});
			reading = mapApiResponse(res);
		} catch (err) {
			error = err instanceof Error ? err.message : '抽牌失敗，請稍後再試';
		} finally {
			loading = false;
		}
	}

	function handleDrawAgain() {
		reading = null;
		error = null;
	}
</script>

<svelte:head>
	<title>塔羅占卜</title>
</svelte:head>

<main>
	<h1>塔羅占卜</h1>

	<SpreadSelector bind:selected={selectedSpread} disabled={loading} />
	<QuestionInput bind:value={question} disabled={loading} />
	<DrawButton onclick={handleDraw} {loading} />

	{#if error}
		<p class="error">{error}</p>
	{/if}

	{#if reading}
		<ReadingDisplay {reading} />
		<div class="actions">
			<button class="action-btn" onclick={handleDrawAgain}>再抽一次</button>
			<a href="/history" class="action-btn secondary">查看歷史</a>
		</div>
	{/if}
</main>

<style>
	main {
		max-width: 800px;
		margin: 0 auto;
		padding: 2rem 1rem;
		font-family: system-ui, -apple-system, sans-serif;
	}

	h1 {
		text-align: center;
		color: #333;
		margin: 0 0 2rem;
	}

	.error {
		text-align: center;
		color: #a03030;
		margin-top: 1rem;
	}

	.actions {
		display: flex;
		justify-content: center;
		gap: 1rem;
		margin-top: 1.5rem;
	}

	.action-btn {
		padding: 0.5rem 1.25rem;
		border: 1px solid #4a3060;
		border-radius: 6px;
		background: #4a3060;
		color: #fff;
		font-size: 0.9rem;
		cursor: pointer;
		font-family: inherit;
		text-decoration: none;
	}

	.action-btn:hover {
		background: #3a2050;
	}

	.action-btn.secondary {
		background: none;
		color: #4a3060;
	}

	.action-btn.secondary:hover {
		background: #f0ecf5;
	}
</style>
