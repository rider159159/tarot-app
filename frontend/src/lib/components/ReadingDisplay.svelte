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
		margin-top: var(--sp-4);
	}

	h2 {
		text-align: center;
		margin-bottom: var(--sp-4);
		color: var(--c-text-1);
	}

	.question {
		text-align: center;
		color: var(--c-text-3);
		margin-bottom: var(--sp-6);
	}

	.feeling-divider {
		border: none;
		border-top: 1px solid var(--c-hairline);
		margin: var(--sp-6) 0;
	}

	.feeling-section {
		text-align: center;
	}

	.feeling-title {
		color: var(--c-accent);
		font-size: var(--fs-body);
		margin-bottom: var(--sp-3);
	}

	footer {
		text-align: center;
		color: var(--c-text-3);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		margin-top: var(--sp-4);
	}
</style>
