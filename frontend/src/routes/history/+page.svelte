<script lang="ts">
	import { enhance } from '$app/forms';
	import { goto } from '$app/navigation';
	import type { ApiReadingResponse } from '$lib/types';
	import { getSpreadName, formatDate } from '$lib/utils/reading';
	import { copyJsonToClipboard, downloadJson, type CopyStatus } from '$lib/utils/clipboard';
	import { getCardImageUrl } from '$lib/tarot';
	import { lightbox } from '$lib/components/lightboxStore.svelte';
	import type { PageData, ActionData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let expandedId: string | null = $state(null);
	let deletingId: string | null = $state(null);
	let copyStatus: Record<string, CopyStatus> = $state({});
	let batchDownloading: boolean = $state(false);

	let totalPages = $derived(Math.ceil(data.totalCount / data.pageSize));
	let formAny = $derived(form as Record<string, any> | null);

	function toggleExpand(id: string) {
		expandedId = expandedId === id ? null : id;
	}

	function goToPage(page: number) {
		goto(`?page=${page}`, { keepFocus: true });
	}

	// 單筆匯出：寫剪貼簿
	$effect(() => {
		if (formAny?.exportSuccess && formAny.exportJson && formAny.exportedId) {
			const id = formAny.exportedId as string;
			copyStatus[id] = 'copying';
			copyJsonToClipboard(formAny.exportJson).then((ok) => {
				copyStatus[id] = ok ? 'done' : 'error';
				setTimeout(() => {
					copyStatus[id] = 'idle';
				}, ok ? 2000 : 3000);
			});
		}
	});

	// 批次匯出：觸發下載
	$effect(() => {
		if (formAny?.exportSuccess && formAny.exportJson && !formAny.exportedId) {
			const today = new Date().toISOString().slice(0, 10);
			downloadJson(formAny.exportJson, `tarot-readings-${today}.json`);
		}
	});
</script>

<svelte:head>
	<title>歷史紀錄 - 塔羅占卜</title>
</svelte:head>

<main>
	<h1>歷史紀錄</h1>

	{#if data.readings.length > 0}
		<section class="batch-export">
			<p class="batch-hint">將你所有的抽牌紀錄匯出成 JSON 檔案，可用於備份或貼給 AI 進行批次解讀。</p>
			<form
				method="POST"
				action="?/exportAll"
				use:enhance={() => {
					batchDownloading = true;
					return async ({ update }) => {
						await update();
						setTimeout(() => {
							batchDownloading = false;
						}, 800);
					};
				}}
			>
				<button type="submit" class="batch-btn" disabled={batchDownloading}>
					{batchDownloading ? '下載中...' : '匯出全部資料'}
				</button>
			</form>
			{#if formAny?.exportSuccess && !formAny?.exportedId}
				<p class="success">
					已下載 {formAny.exportedCount} 筆紀錄
					{#if formAny.isTruncated}
						（共 {formAny.totalCount} 筆，僅匯出最近 1000 筆）
					{/if}
				</p>
			{/if}
		</section>
	{/if}

	{#if form?.error}
		<p class="error">{form.error}</p>
	{/if}
	{#if formAny?.exportError}
		<p class="error">{formAny.exportError}</p>
	{/if}

	{#if data.readings.length === 0}
		<div class="empty">
			<p>還沒有抽牌紀錄，去抽一張吧！</p>
			<a href="/" class="action-btn">前往抽牌</a>
		</div>
	{:else}
		<div class="readings-list">
			{#each data.readings as reading (reading.id)}
				<article class="reading-item">
					<button
						class="reading-header"
						onclick={() => toggleExpand(reading.id)}
						aria-expanded={expandedId === reading.id}
					>
						<div class="reading-meta">
							<span class="date">{formatDate(reading.createdAt)}</span>
							<span class="spread-type">{getSpreadName(reading.spreadType)}</span>
						</div>
						{#if reading.question}
							<p class="question">{reading.question}</p>
						{/if}
						<p class="card-names">
							{reading.cards.map((c) => c.nameCht).join('、')}
						</p>
						<span class="expand-icon">{expandedId === reading.id ? '▲' : '▼'}</span>
					</button>

					{#if expandedId === reading.id}
						<div class="reading-detail">
							{#each reading.cards as card}
								{@const imageUrl = getCardImageUrl(card.cardId)}
								{@const isReversed = card.orientation === 'reversed'}
								<div class="card-detail">
									<div class="card-header">
										<strong>{card.positionLabel}</strong>
										<span class="orientation" class:reversed={isReversed}>
											{isReversed ? '逆位' : '正位'}
										</span>
									</div>
									<div class="card-body">
										{#if imageUrl}
											<button
												type="button"
												class="card-image"
												class:reversed={isReversed}
												aria-label={`放大檢視 ${card.nameCht}`}
												onclick={() => lightbox.open(imageUrl, card.nameCht, isReversed)}
											>
												<img src={imageUrl} alt={card.nameCht} loading="lazy" />
											</button>
										{/if}
										<div class="card-info">
											<p class="card-name">{card.nameCht} <em>{card.name}</em></p>
											<p class="card-meaning">{card.meaning}</p>
											<div class="keywords">
												{#each card.keywords as kw}
													<span class="keyword">{kw}</span>
												{/each}
											</div>
										</div>
									</div>
								</div>
							{/each}
							<div class="detail-actions">
								<form method="POST" action="?/exportSingle" use:enhance>
									<input type="hidden" name="id" value={reading.id} />
									<button
										type="submit"
										class="export-btn"
										disabled={copyStatus[reading.id] === 'copying'}
									>
										{#if copyStatus[reading.id] === 'done'}✓ 已複製
										{:else if copyStatus[reading.id] === 'error'}✗ 複製失敗
										{:else if copyStatus[reading.id] === 'copying'}複製中...
										{:else}複製給 AI{/if}
									</button>
								</form>
								<form
									method="POST"
									action="?/delete"
									use:enhance={({ cancel }) => {
										if (!confirm('確定要刪除這筆抽牌紀錄嗎？')) {
											cancel();
											return;
										}
										deletingId = reading.id;
										return async ({ update }) => {
											deletingId = null;
											expandedId = null;
											await update();
										};
									}}
								>
									<input type="hidden" name="id" value={reading.id} />
									<button
										type="submit"
										class="delete-btn"
										disabled={deletingId === reading.id}
									>
										{deletingId === reading.id ? '刪除中...' : '刪除此紀錄'}
									</button>
								</form>
							</div>
						</div>
					{/if}
				</article>
			{/each}
		</div>

		{#if totalPages > 1}
			<div class="pagination">
				<button
					class="page-btn"
					onclick={() => goToPage(data.currentPage - 1)}
					disabled={data.currentPage <= 1}
				>
					上一頁
				</button>
				<span class="page-info">{data.currentPage} / {totalPages}</span>
				<button
					class="page-btn"
					onclick={() => goToPage(data.currentPage + 1)}
					disabled={data.currentPage >= totalPages}
				>
					下一頁
				</button>
			</div>
		{/if}
	{/if}
</main>

<style>
	main {
		max-width: var(--content-max);
		margin: 0 auto;
		padding: var(--sp-12) var(--content-pad);
	}

	h1 {
		text-align: center;
		font-size: var(--fs-display);
		letter-spacing: var(--ls-display);
		margin: 0 0 var(--sp-12);
	}

	.error {
		text-align: center;
		color: var(--c-error);
	}

	.empty {
		text-align: center;
		color: var(--c-text-3);
		padding: var(--sp-12) 0;
	}

	.empty p {
		font-size: var(--fs-h3);
		margin-bottom: var(--sp-6);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.action-btn {
		display: inline-block;
		padding: var(--sp-2) var(--sp-6);
		background: transparent;
		color: var(--c-text-1);
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		text-decoration: none;
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		transition: border-color var(--transition), color var(--transition);
	}

	.action-btn:hover {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.readings-list {
		display: flex;
		flex-direction: column;
		gap: var(--sp-3);
	}

	.reading-item {
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		overflow: hidden;
	}

	.reading-header {
		width: 100%;
		background: transparent;
		border: none;
		padding: var(--sp-4);
		cursor: pointer;
		text-align: left;
		font-family: inherit;
		color: inherit;
		position: relative;
		transition: background var(--transition);
	}

	.reading-header:hover {
		background: var(--c-surface-1);
	}

	.reading-meta {
		display: flex;
		align-items: center;
		gap: var(--sp-3);
		margin-bottom: var(--sp-1);
	}

	.date {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
	}

	.spread-type {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		background: var(--c-surface-2);
		color: var(--c-text-2);
		padding: var(--sp-1) var(--sp-2);
		border-radius: var(--radius-0);
	}

	.question {
		font-size: var(--fs-sm);
		color: var(--c-text-3);
		margin: var(--sp-1) 0;
	}

	.card-names {
		font-size: var(--fs-sm);
		color: var(--c-text-1);
		margin: var(--sp-1) 0 0;
	}

	.expand-icon {
		position: absolute;
		right: var(--sp-4);
		top: var(--sp-4);
		font-size: var(--fs-xs);
		color: var(--c-text-3);
	}

	.reading-detail {
		border-top: 1px solid var(--c-hairline);
		padding: var(--sp-4);
		background: var(--c-surface-1);
	}

	.card-detail {
		padding: var(--sp-3) 0;
		border-bottom: 1px solid var(--c-hairline);
	}

	.card-detail:last-of-type {
		border-bottom: none;
	}

	.card-header {
		display: flex;
		align-items: center;
		gap: var(--sp-2);
		margin-bottom: var(--sp-2);
	}

	.card-body {
		display: flex;
		gap: var(--sp-4);
		align-items: flex-start;
	}

	/* card-image 是 <button>：重置按鈕預設樣式，點擊開 lightbox */
	.card-image {
		flex-shrink: 0;
		width: 90px;
		display: block;
		padding: 0;
		margin: 0;
		background: transparent;
		font: inherit;
		cursor: zoom-in;
		/* 左側 2px 標記條：正逆位視覺訊號 */
		border: none;
		border-left: 2px solid var(--c-accent);
		padding-left: var(--sp-2);
	}

	.card-image.reversed {
		transform: rotate(180deg);
		border-left-color: var(--c-accent-dim);
	}

	.card-image img {
		width: 100%;
		height: auto;
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
	}

	.card-image.reversed img {
		filter: brightness(0.82) saturate(0.9);
	}

	.card-info {
		flex: 1;
		min-width: 0;
	}

	/* 方向標籤：mono 字體，像「狀態標記」 */
	.orientation {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-accent);
	}

	.orientation.reversed {
		color: var(--c-accent-dim);
	}

	.card-name {
		margin: var(--sp-1) 0;
		font-size: var(--fs-body);
		color: var(--c-text-1);
	}

	.card-name em {
		color: var(--c-text-3);
		font-size: var(--fs-sm);
	}

	.card-meaning {
		font-size: var(--fs-sm);
		color: var(--c-text-2);
		line-height: var(--lh-body);
		margin: var(--sp-1) 0;
	}

	.keywords {
		display: flex;
		flex-wrap: wrap;
		gap: var(--sp-1);
		margin-top: var(--sp-1);
	}

	.keyword {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		background: var(--c-surface-2);
		color: var(--c-text-3);
		padding: var(--sp-1) var(--sp-2);
		border-radius: var(--radius-0);
	}

	.detail-actions {
		display: flex;
		flex-wrap: wrap;
		gap: var(--sp-2);
		margin-top: var(--sp-4);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill（刪除/匯出/分頁共用）*/
	.delete-btn,
	.export-btn,
	.page-btn {
		padding: var(--sp-1) var(--sp-3);
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	.delete-btn:hover:not(:disabled),
	.export-btn:hover:not(:disabled),
	.page-btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	/* 刪除鍵 hover 用柔粉，提示破壞性操作 */
	.delete-btn:hover:not(:disabled) {
		border-color: var(--c-error);
		color: var(--c-error);
	}

	.delete-btn:disabled,
	.export-btn:disabled,
	.page-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.batch-export {
		border: 1px solid var(--c-hairline);
		background: var(--c-surface-1);
		border-radius: var(--radius-0);
		padding: var(--sp-4);
		margin-bottom: var(--sp-6);
		text-align: center;
	}

	.batch-hint {
		font-size: var(--fs-sm);
		color: var(--c-text-3);
		margin: 0 0 var(--sp-3);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.batch-btn {
		padding: var(--sp-2) var(--sp-6);
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	.batch-btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.batch-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.success {
		color: var(--c-success);
		font-size: var(--fs-sm);
		margin: var(--sp-2) 0 0;
	}

	.pagination {
		display: flex;
		justify-content: center;
		align-items: center;
		gap: var(--sp-4);
		margin-top: var(--sp-8);
	}

	.page-info {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
	}
</style>
