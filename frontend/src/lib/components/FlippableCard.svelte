<script lang="ts">
	// 單張可翻牌：牌背 → 點擊 → 3D rotateY 翻面顯示牌正面照片（含正逆位）。
	// 牌海上只翻照片；完整牌義在選滿後由結果頁的 ReadingDisplay 呈現。
	import type { DrawnCard } from '$lib/types';
	import { getCardImageUrl } from '$lib/tarot';

	let {
		face = null,
		flipped = false,
		selected = false,
		disabled = false,
		onpick
	}: {
		face?: DrawnCard | null;
		flipped?: boolean;
		selected?: boolean;
		disabled?: boolean;
		onpick?: () => void;
	} = $props();

	let imageError = $state(false);
	const isReversed = $derived(face?.orientation === 'reversed');
	const imageUrl = $derived(face ? getCardImageUrl(face.card.id) : undefined);
</script>

<button
	type="button"
	class="flip-card"
	class:flipped
	class:selected
	disabled={disabled || flipped}
	aria-label={flipped && face ? face.card.nameCht : '選這張牌'}
	onclick={() => onpick?.()}
>
	<div class="flip-inner">
		<!-- 牌背 -->
		<div class="flip-face flip-back">
			<img src="/cards/card-back.svg" alt="" aria-hidden="true" />
		</div>
		<!-- 牌正面：照片，逆位整張旋轉 180° -->
		<div class="flip-face flip-front" class:reversed={isReversed}>
			{#if face}
				{#if imageUrl && !imageError}
					<img
						src={imageUrl}
						alt={face.card.nameCht}
						onerror={() => (imageError = true)}
					/>
				{:else}
					<span class="front-fallback">{face.card.nameCht}</span>
				{/if}
			{/if}
		</div>
	</div>
</button>

<style>
	.flip-card {
		display: block;
		padding: 0;
		margin: 0;
		border: none;
		background: transparent;
		width: 100%;
		aspect-ratio: 220 / 380;
		cursor: pointer;
		perspective: 900px;
	}

	/* 抬起 / 位移交給外層容器（如 CardPicker 的 .slot）統一處理，
	   避免兩層 transform 互相覆蓋。 */
	.flip-card:disabled {
		cursor: default;
	}

	.flip-inner {
		position: relative;
		width: 100%;
		height: 100%;
		transform-style: preserve-3d;
		transition: transform 0.55s cubic-bezier(0.4, 0, 0.2, 1);
	}

	.flip-card.flipped .flip-inner {
		transform: rotateY(180deg);
	}

	.flip-face {
		position: absolute;
		inset: 0;
		backface-visibility: hidden;
		-webkit-backface-visibility: hidden;
		border-radius: var(--radius-0);
		overflow: hidden;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.flip-face img {
		width: 100%;
		height: 100%;
		object-fit: cover;
		display: block;
	}

	.flip-back {
		transform: rotateY(0deg);
	}

	.flip-card.selected .flip-back {
		outline: 2px solid var(--c-accent);
		outline-offset: -2px;
	}

	.flip-front {
		transform: rotateY(180deg);
		background: var(--c-surface-2);
		border: 1px solid var(--c-hairline);
	}

	/* 逆位：整張牌面旋轉 180°，微暗化呼應逆位語意 */
	.flip-front.reversed img {
		transform: rotate(180deg);
		filter: brightness(0.82) saturate(0.9);
	}

	.front-fallback {
		color: var(--c-text-1);
		font-size: var(--fs-sm);
		text-align: center;
		padding: var(--sp-2);
	}
</style>
