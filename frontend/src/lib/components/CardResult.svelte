<script lang="ts">
	import type { DrawnCard } from '$lib/types';
	import { getCardImageUrl } from '$lib/tarot';
	import { lightbox } from './lightboxStore.svelte';

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
			<button
				type="button"
				class="card-image"
				class:reversed={isReversed}
				aria-label={`放大檢視 ${drawnCard.card.nameCht}`}
				onclick={() => lightbox.open(imageUrl!, drawnCard.card.nameCht, isReversed)}
			>
				<img
					src={imageUrl}
					alt={drawnCard.card.nameCht}
					loading="lazy"
					onerror={() => (imageError = true)}
				/>
			</button>
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
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		padding: var(--sp-4);
		margin-bottom: var(--sp-4);
	}

	header {
		margin-bottom: var(--sp-3);
		padding-bottom: var(--sp-2);
		border-bottom: 1px solid var(--c-hairline);
	}

	.position-label {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		color: var(--c-accent);
		letter-spacing: var(--ls-mono);
	}

	.position-desc {
		display: block;
		color: var(--c-text-3);
		font-size: var(--fs-xs);
		margin-top: var(--sp-1);
	}

	.card-body {
		display: flex;
		gap: var(--sp-4);
		align-items: flex-start;
	}

	/* card-image 是 <button>：重置按鈕預設樣式，點擊開 lightbox */
	.card-image {
		flex-shrink: 0;
		width: 150px;
		display: block;
		padding: 0;
		margin: 0;
		background: transparent;
		font: inherit;
		cursor: zoom-in;
		/* 左側 2px 標記條：正逆位視覺訊號（不靠明度差也能辨識） */
		border: none;
		border-left: 2px solid var(--c-accent);
		padding-left: var(--sp-3);
	}

	.card-image.reversed {
		transform: rotate(180deg);
		border-left-color: var(--c-accent-dim);
	}

	.card-image img {
		width: 100%;
		height: auto;
		/* RWS 米白卡圖在深色底用 hairline 收邊，避免「浮空」 */
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
	}

	/* 逆位卡圖微暗化，呼應逆位語意、柔化米白底在純黑上的刺眼 */
	.card-image.reversed img {
		filter: brightness(0.82) saturate(0.9);
	}

	.card-image-fallback {
		flex-shrink: 0;
		width: 150px;
		height: 260px;
		background: var(--c-surface-2);
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		display: flex;
		align-items: center;
		justify-content: center;
		color: var(--c-text-1);
		font-size: var(--fs-h3);
		text-align: center;
		padding: var(--sp-4);
	}

	.card-image-fallback.reversed {
		transform: rotate(180deg);
	}

	.card-info {
		flex: 1;
		min-width: 0;
	}

	h3 {
		margin: 0 0 var(--sp-1);
		font-size: var(--fs-h3);
	}

	/* 方向標籤：mono 字體，像「狀態標記」 */
	.orientation {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-accent);
	}

	.orientation.reversed {
		color: var(--c-accent-dim);
	}

	.english-name {
		color: var(--c-text-3);
		font-size: var(--fs-sm);
		font-style: italic;
		margin: 0 0 var(--sp-2);
	}

	.meaning {
		line-height: var(--lh-body);
		color: var(--c-text-2);
		margin: 0 0 var(--sp-3);
	}

	.keywords {
		display: flex;
		flex-wrap: wrap;
		gap: var(--sp-2);
	}

	.keyword {
		background: var(--c-surface-2);
		color: var(--c-text-3);
		font-family: var(--font-mono);
		padding: var(--sp-1) var(--sp-3);
		border-radius: var(--radius-0);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
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
