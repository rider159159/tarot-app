<script lang="ts">
	import type { ReadingResult } from '$lib/types';
	import { spreadConfigs } from '$lib/tarot';
	import CardResult from './CardResult.svelte';

	let { reading }: { reading: ReadingResult } = $props();

	const config = $derived(spreadConfigs[reading.spreadType]);
</script>

<section class="reading-display">
	<h2>{config.name} — 解讀結果</h2>
	{#if reading.question}
		<p class="question">問題：{reading.question}</p>
	{/if}
	<div class="cards-list">
		{#each reading.cards as drawnCard (drawnCard.position.index)}
			<CardResult {drawnCard} />
		{/each}
	</div>
	<footer>
		<time>{new Date(reading.createdAt).toLocaleString('zh-TW')}</time>
	</footer>
</section>

<style>
	.reading-display {
		margin-top: 1rem;
	}

	h2 {
		text-align: center;
		margin-bottom: 1rem;
		color: #4a3060;
	}

	.question {
		text-align: center;
		font-style: italic;
		color: #555;
		margin-bottom: 1.5rem;
	}

	footer {
		text-align: center;
		color: #999;
		font-size: 0.8rem;
		margin-top: 1rem;
	}
</style>
