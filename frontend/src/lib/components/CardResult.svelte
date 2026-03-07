<script lang="ts">
	import type { DrawnCard } from '$lib/types';
	import { getCardImageUrl } from '$lib/tarot';

	let { drawnCard }: { drawnCard: DrawnCard } = $props();

	const isReversed = $derived(drawnCard.orientation === 'reversed');
	const meaning = $derived(
		isReversed ? drawnCard.card.meaningReversed : drawnCard.card.meaningUpright
	);
	const imageUrl = $derived(getCardImageUrl(drawnCard.card.id));

	let imageError = $state(false);
</script>

<article class="card-result">
	<header>
		<span class="position-label">{drawnCard.position.label}</span>
		<small class="position-desc">{drawnCard.position.description}</small>
	</header>
	<div class="card-body">
		{#if imageUrl && !imageError}
			<div class="card-image" class:reversed={isReversed}>
				<img
					src={imageUrl}
					alt={drawnCard.card.nameCht}
					loading="lazy"
					onerror={() => (imageError = true)}
				/>
			</div>
		{:else}
			<div class="card-image-fallback" class:reversed={isReversed}>
				<span>{drawnCard.card.nameCht}</span>
			</div>
		{/if}
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

	.card-body {
		display: flex;
		gap: 1rem;
		align-items: flex-start;
	}

	.card-image {
		flex-shrink: 0;
		width: 150px;
	}

	.card-image.reversed {
		transform: rotate(180deg);
	}

	.card-image img {
		width: 100%;
		height: auto;
		border-radius: 6px;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
	}

	.card-image-fallback {
		flex-shrink: 0;
		width: 150px;
		height: 260px;
		background: linear-gradient(135deg, #4a3060, #6b4d8a);
		border-radius: 6px;
		display: flex;
		align-items: center;
		justify-content: center;
		color: #fff;
		font-size: 1.25rem;
		font-weight: bold;
		text-align: center;
		padding: 1rem;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
	}

	.card-image-fallback.reversed {
		transform: rotate(180deg);
	}

	.card-info {
		flex: 1;
		min-width: 0;
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

	@media (max-width: 480px) {
		.card-body {
			flex-direction: column;
			align-items: center;
		}

		.card-image,
		.card-image-fallback {
			width: 120px;
		}

		.card-image-fallback {
			height: 210px;
		}
	}
</style>
