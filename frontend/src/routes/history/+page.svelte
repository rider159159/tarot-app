<script lang="ts">
	import { enhance } from '$app/forms';
	import { goto } from '$app/navigation';
	import type { ApiReadingResponse } from '$lib/types';
	import { getSpreadName, formatDate } from '$lib/utils/reading';
	import { getCardImageUrl } from '$lib/tarot';
	import type { PageData, ActionData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let expandedId: string | null = $state(null);
	let deletingId: string | null = $state(null);

	let totalPages = $derived(Math.ceil(data.totalCount / data.pageSize));

	function toggleExpand(id: string) {
		expandedId = expandedId === id ? null : id;
	}

	function goToPage(page: number) {
		goto(`?page=${page}`, { keepFocus: true });
	}
</script>

<svelte:head>
	<title>歷史紀錄 - 塔羅占卜</title>
</svelte:head>

<main>
	<h1>歷史紀錄</h1>

	{#if form?.error}
		<p class="error">{form.error}</p>
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
											<div class="card-image" class:reversed={isReversed}>
												<img src={imageUrl} alt={card.nameCht} loading="lazy" />
											</div>
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
	}

	.empty {
		text-align: center;
		color: #666;
		padding: 3rem 0;
	}

	.empty p {
		font-size: 1.1rem;
		margin-bottom: 1.5rem;
	}

	.action-btn {
		display: inline-block;
		padding: 0.5rem 1.25rem;
		background: #4a3060;
		color: #fff;
		border-radius: 6px;
		text-decoration: none;
		font-size: 0.9rem;
	}

	.action-btn:hover {
		background: #3a2050;
	}

	.readings-list {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.reading-item {
		border: 1px solid #ddd;
		border-radius: 8px;
		overflow: hidden;
	}

	.reading-header {
		width: 100%;
		background: none;
		border: none;
		padding: 1rem;
		cursor: pointer;
		text-align: left;
		font-family: inherit;
		position: relative;
	}

	.reading-header:hover {
		background: #faf8fc;
	}

	.reading-meta {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin-bottom: 0.25rem;
	}

	.date {
		font-size: 0.85rem;
		color: #666;
	}

	.spread-type {
		font-size: 0.8rem;
		background: #f0ecf5;
		color: #4a3060;
		padding: 0.125rem 0.5rem;
		border-radius: 4px;
	}

	.question {
		font-size: 0.9rem;
		color: #555;
		margin: 0.25rem 0;
		font-style: italic;
	}

	.card-names {
		font-size: 0.9rem;
		color: #333;
		margin: 0.25rem 0 0;
	}

	.expand-icon {
		position: absolute;
		right: 1rem;
		top: 1rem;
		font-size: 0.75rem;
		color: #999;
	}

	.reading-detail {
		border-top: 1px solid #eee;
		padding: 1rem;
		background: #faf8fc;
	}

	.card-detail {
		padding: 0.75rem 0;
		border-bottom: 1px solid #eee;
	}

	.card-detail:last-of-type {
		border-bottom: none;
	}

	.card-header {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		margin-bottom: 0.25rem;
	}

	.orientation {
		font-size: 0.8rem;
		color: #4a3060;
	}

	.orientation.reversed {
		color: #a03030;
	}

	.card-name {
		margin: 0.25rem 0;
		font-size: 0.95rem;
	}

	.card-name em {
		color: #888;
		font-size: 0.85rem;
	}

	.card-meaning {
		font-size: 0.875rem;
		color: #555;
		line-height: 1.6;
		margin: 0.25rem 0;
	}

	.keywords {
		display: flex;
		flex-wrap: wrap;
		gap: 0.375rem;
		margin-top: 0.375rem;
	}

	.keyword {
		font-size: 0.75rem;
		background: #f0ecf5;
		color: #4a3060;
		padding: 0.125rem 0.5rem;
		border-radius: 10px;
	}

	.delete-btn {
		margin-top: 1rem;
		padding: 0.375rem 0.75rem;
		background: none;
		border: 1px solid #a03030;
		border-radius: 6px;
		color: #a03030;
		font-size: 0.8rem;
		cursor: pointer;
		font-family: inherit;
	}

	.delete-btn:hover:not(:disabled) {
		background: #a03030;
		color: #fff;
	}

	.delete-btn:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.pagination {
		display: flex;
		justify-content: center;
		align-items: center;
		gap: 1rem;
		margin-top: 2rem;
	}

	.page-btn {
		padding: 0.375rem 0.75rem;
		border: 1px solid #4a3060;
		border-radius: 6px;
		background: none;
		color: #4a3060;
		font-size: 0.85rem;
		cursor: pointer;
		font-family: inherit;
	}

	.page-btn:hover:not(:disabled) {
		background: #4a3060;
		color: #fff;
	}

	.page-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.page-info {
		font-size: 0.85rem;
		color: #666;
	}
</style>
