<script lang="ts">
	import { enhance } from '$app/forms';
	import { page } from '$app/stores';
	import { getSpreadName, formatDate } from '$lib/utils/reading';
	import type { PageData, ActionData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let editing: boolean = $state(false);
	let editName: string = $state('');
	let saving: boolean = $state(false);

	let email = $derived($page.data.user?.email ?? '');

	// Reflect successful name update into local state
	let displayName = $derived(form?.profile?.displayName ?? data.profile.displayName);

	function startEdit() {
		editName = displayName;
		editing = true;
	}

	function cancelEdit() {
		editing = false;
	}
</script>

<svelte:head>
	<title>個人頁面 - 塔羅占卜</title>
</svelte:head>

<main>
	<h1>個人頁面</h1>

	<section class="profile-section">
		<h2>個人資料</h2>
		<div class="info-row">
			<span class="label">Email</span>
			<span class="value">{email}</span>
		</div>
		<div class="info-row">
			<span class="label">顯示名稱</span>
			{#if editing}
				<form
					method="POST"
					action="?/updateName"
					class="edit-group"
					use:enhance={() => {
						saving = true;
						return async ({ update, result }) => {
							saving = false;
							if (result.type === 'success') {
								editing = false;
							}
							await update();
						};
					}}
				>
					<input
						type="text"
						name="displayName"
						bind:value={editName}
						disabled={saving}
						class="edit-input"
					/>
					<button type="submit" class="save-btn" disabled={saving}>
						{saving ? '儲存中...' : '儲存'}
					</button>
					<button type="button" class="cancel-btn" onclick={cancelEdit} disabled={saving}>
						取消
					</button>
				</form>
			{:else}
				<span class="value">
					{displayName}
					<button class="edit-btn" onclick={startEdit}>編輯</button>
				</span>
			{/if}
		</div>
		{#if form?.error}
			<p class="error">{form.error}</p>
		{/if}
	</section>

	<section class="stats-section">
		<h2>統計資料</h2>
		<div class="stat-card">
			<span class="stat-label">總抽牌次數</span>
			<span class="stat-value">{data.stats.totalCount} 次</span>
		</div>

		{#if data.stats.lastReadingAt}
			<div class="stat-card">
				<span class="stat-label">最近一次抽牌</span>
				<span class="stat-value">{formatDate(data.stats.lastReadingAt)}</span>
			</div>
		{/if}

		{#if data.stats.spreadUsage.length > 0}
			<div class="stat-card">
				<span class="stat-label">牌陣使用次數</span>
				<ul class="stat-list">
					{#each data.stats.spreadUsage as usage}
						<li>{getSpreadName(usage.spreadType)}：{usage.count} 次</li>
					{/each}
				</ul>
			</div>
		{/if}

		{#if data.stats.topCards.length > 0}
			<div class="stat-card">
				<span class="stat-label">最常抽到的牌 Top 5</span>
				<ol class="stat-list ranked">
					{#each data.stats.topCards as card}
						<li>{card.nameCht}：{card.count} 次</li>
					{/each}
				</ol>
			</div>
		{/if}
	</section>

	<section class="logout-section">
		<form method="POST" action="/auth/logout">
			<button type="submit" class="logout-btn">登出</button>
		</form>
	</section>
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

	h2 {
		color: #4a3060;
		font-size: 1.1rem;
		margin: 0 0 1rem;
	}

	.error {
		color: #a03030;
		font-size: 0.85rem;
		margin-top: 0.5rem;
	}

	section {
		border: 1px solid #ddd;
		border-radius: 8px;
		padding: 1.25rem;
		margin-bottom: 1.5rem;
	}

	.info-row {
		display: flex;
		align-items: flex-start;
		gap: 1rem;
		padding: 0.5rem 0;
		border-bottom: 1px solid #eee;
	}

	.info-row:last-of-type {
		border-bottom: none;
	}

	.label {
		font-size: 0.85rem;
		color: #666;
		min-width: 5rem;
		padding-top: 0.25rem;
	}

	.value {
		font-size: 0.95rem;
		color: #333;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.edit-btn {
		padding: 0.125rem 0.5rem;
		background: none;
		border: 1px solid #4a3060;
		border-radius: 4px;
		color: #4a3060;
		font-size: 0.75rem;
		cursor: pointer;
		font-family: inherit;
	}

	.edit-btn:hover {
		background: #f0ecf5;
	}

	.edit-group {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.edit-input {
		padding: 0.375rem 0.5rem;
		border: 1px solid #ddd;
		border-radius: 6px;
		font-size: 0.9rem;
		font-family: inherit;
		width: 12rem;
	}

	.edit-input:focus {
		outline: none;
		border-color: #4a3060;
		box-shadow: 0 0 0 2px rgba(74, 48, 96, 0.1);
	}

	.save-btn {
		padding: 0.375rem 0.75rem;
		background: #4a3060;
		border: none;
		border-radius: 6px;
		color: #fff;
		font-size: 0.8rem;
		cursor: pointer;
		font-family: inherit;
	}

	.save-btn:hover:not(:disabled) {
		background: #3a2050;
	}

	.save-btn:disabled {
		opacity: 0.5;
	}

	.cancel-btn {
		padding: 0.375rem 0.75rem;
		background: none;
		border: 1px solid #999;
		border-radius: 6px;
		color: #666;
		font-size: 0.8rem;
		cursor: pointer;
		font-family: inherit;
	}

	.cancel-btn:hover:not(:disabled) {
		border-color: #333;
		color: #333;
	}

	.stat-card {
		padding: 0.75rem 0;
		border-bottom: 1px solid #eee;
	}

	.stat-card:last-child {
		border-bottom: none;
	}

	.stat-label {
		font-size: 0.85rem;
		color: #666;
		display: block;
		margin-bottom: 0.25rem;
	}

	.stat-value {
		font-size: 1rem;
		color: #333;
		font-weight: 500;
	}

	.stat-list {
		margin: 0.5rem 0 0;
		padding-left: 1.25rem;
		font-size: 0.9rem;
		color: #333;
	}

	.stat-list li {
		padding: 0.125rem 0;
	}

	.logout-section {
		text-align: center;
		border: none;
		padding: 0;
	}

	.logout-btn {
		padding: 0.5rem 1.5rem;
		background: none;
		border: 1px solid #a03030;
		border-radius: 6px;
		color: #a03030;
		font-size: 0.9rem;
		cursor: pointer;
		font-family: inherit;
	}

	.logout-btn:hover {
		background: #a03030;
		color: #fff;
	}
</style>
