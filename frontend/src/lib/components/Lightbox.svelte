<script lang="ts">
	import { lightbox } from './lightboxStore.svelte';

	const image = $derived(lightbox.image);

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') lightbox.close();
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if image}
	<div class="lightbox-overlay" role="dialog" aria-modal="true" aria-label="放大檢視塔羅牌">
		<!-- 背景按鈕：點背景任意處關閉（圖片與關閉鍵疊在其上）-->
		<button type="button" class="backdrop" aria-label="關閉" onclick={() => lightbox.close()}
		></button>
		<button type="button" class="close-btn" aria-label="關閉" onclick={() => lightbox.close()}>
			✕
		</button>
		<figure class="content">
			<img src={image.src} alt={image.alt} class:reversed={image.reversed} />
			<figcaption class="caption">
				{image.alt}{image.reversed ? '（逆位）' : ''}
			</figcaption>
		</figure>
	</div>
{/if}

<style>
	.lightbox-overlay {
		position: fixed;
		inset: 0;
		z-index: 2000;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: var(--sp-8);
	}

	/* 全幅背景按鈕：點任意處關閉 */
	.backdrop {
		position: absolute;
		inset: 0;
		background: rgba(14, 10, 20, 0.92);
		border: none;
		padding: 0;
		cursor: zoom-out;
	}

	.content {
		position: relative;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--sp-4);
		margin: 0;
	}

	img {
		max-width: min(90vw, 520px);
		max-height: 80vh;
		width: auto;
		height: auto;
		object-fit: contain;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-0);
	}

	img.reversed {
		transform: rotate(180deg);
	}

	.caption {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-2);
		margin: 0;
		text-align: center;
	}

	/* 按鈕樣板：透明底 + 1px 外框 + 圓形 */
	.close-btn {
		position: absolute;
		top: var(--sp-6);
		right: var(--sp-6);
		width: 40px;
		height: 40px;
		display: flex;
		align-items: center;
		justify-content: center;
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-body);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	.close-btn:hover {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}
</style>
