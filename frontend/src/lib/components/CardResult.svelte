<script lang="ts">
	import type { DrawnCard } from '$lib/types';

	let { drawnCard }: { drawnCard: DrawnCard } = $props();

	const isReversed = $derived(drawnCard.orientation === 'reversed');
	const meaning = $derived(
		isReversed ? drawnCard.card.meaningReversed : drawnCard.card.meaningUpright
	);
</script>

<article class="card-result">
	<header>
		<span class="position-label">{drawnCard.position.label}</span>
		<small class="position-desc">{drawnCard.position.description}</small>
	</header>
	<div class="card-info">
		<h3>
			{drawnCard.card.nameCht}
			<span class="orientation" class:reversed={isReversed}>
				{isReversed ? '（逆位）' : '（正位）'}
			</span>
		</h3>
		<p class="english-name">{drawnCard.card.name}</p>
		<p class="meaning">{meaning}</p>
		<div class="keywords">
			{#each drawnCard.card.keywords as kw}
				<span class="keyword">{kw}</span>
			{/each}
		</div>
	</div>
</article>

<style>
	.card-result {
		border: 1px solid #ddd;
		border-radius: 8px;
		padding: 1rem;
		margin-bottom: 1rem;
	}

	header {
		margin-bottom: 0.75rem;
		padding-bottom: 0.5rem;
		border-bottom: 1px solid #eee;
	}

	.position-label {
		font-weight: bold;
		font-size: 0.875rem;
		color: #4a3060;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.position-desc {
		display: block;
		color: #888;
		font-size: 0.8rem;
		margin-top: 0.25rem;
	}

	h3 {
		margin: 0 0 0.25rem;
		font-size: 1.125rem;
	}

	.orientation {
		font-weight: normal;
		font-size: 0.9rem;
		color: #4a3060;
	}

	.orientation.reversed {
		color: #a03030;
	}

	.english-name {
		color: #888;
		font-size: 0.85rem;
		font-style: italic;
		margin: 0 0 0.5rem;
	}

	.meaning {
		line-height: 1.6;
		margin: 0 0 0.75rem;
	}

	.keywords {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.keyword {
		background-color: #f0ecf5;
		color: #4a3060;
		padding: 0.2rem 0.6rem;
		border-radius: 999px;
		font-size: 0.8rem;
	}
</style>
