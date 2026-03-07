<script lang="ts">
	import type { ReadingResult } from '$lib/types';
	import { spreadConfigs } from '$lib/tarot';
	import { getSpreadName } from '$lib/utils/reading';
	import CardResult from './CardResult.svelte';

	let { reading }: { reading: ReadingResult } = $props();

	const config = $derived(spreadConfigs[reading.spreadType as keyof typeof spreadConfigs]);
	const spreadName = $derived(config?.name ?? getSpreadName(reading.spreadType));
	const mainCards = $derived(reading.cards.filter((c) => c.position.label !== '你的感受'));
	const feelingCard = $derived(reading.cards.find((c) => c.position.label === '你的感受'));
</script>

<section class="reading-display">
	<h2>{spreadName} — 解讀結果</h2>
	{#if reading.question}
		<p class="question">問題：{reading.question}</p>
	{/if}
	<div class="cards-list">
		{#each mainCards as drawnCard (drawnCard.position.index)}
			<CardResult {drawnCard} />
		{/each}
	</div>
	{#if feelingCard}
		<hr class="feeling-divider" />
		<div class="feeling-section">
			<h3 class="feeling-title">你的感受</h3>
			<CardResult drawnCard={feelingCard} />
		</div>
	{/if}
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

	.feeling-divider {
		border: none;
		border-top: 1px dashed #c0b0d0;
		margin: 1.5rem 0;
	}

	.feeling-section {
		text-align: center;
	}

	.feeling-title {
		color: #7a5a90;
		font-size: 1rem;
		margin-bottom: 0.75rem;
	}

	footer {
		text-align: center;
		color: #999;
		font-size: 0.8rem;
		margin-top: 1rem;
	}
</style>
