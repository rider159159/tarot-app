<script lang="ts">
	import { enhance } from '$app/forms';
	import { page } from '$app/stores';
	import { getSpreadName, formatDate, mapApiResponse } from '$lib/utils/reading';
	import { copyJsonToClipboard, type CopyStatus } from '$lib/utils/clipboard';
	import type { PageData, ActionData } from './$types';
	import ReadingDisplay from '$lib/components/ReadingDisplay.svelte';
	import Seo from '$lib/components/Seo.svelte';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let editing: boolean = $state(false);
	let editName: string = $state('');
	let saving: boolean = $state(false);
	let drawingWeekly: boolean = $state(false);
	let copyStatus: CopyStatus = $state('idle');

	let email = $derived($page.data.user?.email ?? '');

	// Reflect successful name update into local state
	let displayName = $derived(form?.profile?.displayName ?? data.profile.displayName);

	// Weekly fortune: prefer action result over load data
	let formAny = $derived(form as Record<string, any> | null);
	let weeklyReading = $derived(
		formAny?.weeklyReading
			? mapApiResponse(formAny.weeklyReading)
			: data.weeklyFortune.reading
				? mapApiResponse(data.weeklyFortune.reading)
				: null
	);
	let canDrawWeekly = $derived(formAny?.weeklyReading ? false : data.weeklyFortune.canDraw);

	function startEdit() {
		editName = displayName;
		editing = true;
	}

	function cancelEdit() {
		editing = false;
	}

	$effect(() => {
		if (formAny?.exportSuccess && formAny.exportJson && formAny.exportedId) {
			copyStatus = 'copying';
			copyJsonToClipboard(formAny.exportJson).then((ok) => {
				copyStatus = ok ? 'done' : 'error';
				setTimeout(() => {
					copyStatus = 'idle';
				}, ok ? 2000 : 3000);
			});
		}
	});
</script>

<Seo title="個人頁面" description="管理你的塔羅占卜帳號與統計資料。" noindex />

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

	<section class="weekly-section">
		<h2>每週週運</h2>
		{#if weeklyReading}
			<ReadingDisplay reading={weeklyReading} />
			{#if weeklyReading.id}
				<form method="POST" action="?/exportSingle" use:enhance class="export-form">
					<input type="hidden" name="id" value={weeklyReading.id} />
					<button
						type="submit"
						class="export-btn"
						disabled={copyStatus === 'copying'}
					>
						{#if copyStatus === 'done'}✓ 已複製
						{:else if copyStatus === 'error'}✗ 複製失敗
						{:else if copyStatus === 'copying'}複製中...
						{:else}複製給 AI{/if}
					</button>
				</form>
			{/if}
			{#if formAny?.exportError}
				<p class="error">{formAny.exportError}</p>
			{/if}
		{:else if canDrawWeekly}
			<p class="weekly-hint">抽出三張塔羅牌，看看本週的能量、挑戰與建議</p>
			<form
				method="POST"
				action="?/drawWeekly"
				use:enhance={() => {
					drawingWeekly = true;
					return async ({ update }) => {
						drawingWeekly = false;
						await update();
					};
				}}
			>
				<button type="submit" class="draw-weekly-btn" disabled={drawingWeekly}>
					{drawingWeekly ? '抽牌中...' : '抽取本週週運'}
				</button>
			</form>
		{:else}
			<p class="weekly-done">本週已抽過週運，下週一可再次抽取</p>
		{/if}
		{#if formAny?.weeklyError}
			<p class="error">{formAny.weeklyError}</p>
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

	h2 {
		color: var(--c-accent);
		font-size: var(--fs-h3);
		margin: 0 0 var(--sp-4);
	}

	.error {
		color: var(--c-error);
		font-size: var(--fs-sm);
		margin-top: var(--sp-2);
	}

	section {
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		padding: var(--sp-6);
		margin-bottom: var(--sp-6);
	}

	.info-row {
		display: flex;
		align-items: flex-start;
		gap: var(--sp-4);
		padding: var(--sp-2) 0;
		border-bottom: 1px solid var(--c-hairline);
	}

	.info-row:last-of-type {
		border-bottom: none;
	}

	.label {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
		min-width: 5rem;
		padding-top: var(--sp-1);
	}

	.value {
		font-size: var(--fs-body);
		color: var(--c-text-1);
		display: flex;
		align-items: center;
		gap: var(--sp-2);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill（編輯/儲存/取消/匯出/登出共用）*/
	.edit-btn,
	.save-btn,
	.cancel-btn,
	.export-btn,
	.logout-btn {
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

	.edit-btn:hover:not(:disabled),
	.save-btn:hover:not(:disabled),
	.cancel-btn:hover:not(:disabled),
	.export-btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.save-btn:disabled,
	.export-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.edit-group {
		display: flex;
		align-items: center;
		gap: var(--sp-2);
	}

	.edit-input {
		padding: var(--sp-2);
		font-family: var(--font-serif);
		font-size: var(--fs-sm);
		width: 12rem;
	}

	.weekly-section {
		background: var(--c-surface-1);
		border-color: var(--c-hairline);
	}

	.weekly-hint {
		text-align: center;
		color: var(--c-text-3);
		font-size: var(--fs-sm);
		margin: 0 0 var(--sp-4);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.draw-weekly-btn {
		display: block;
		width: 100%;
		padding: var(--sp-3);
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-body);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	.draw-weekly-btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.draw-weekly-btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.weekly-done {
		text-align: center;
		color: var(--c-text-3);
		font-size: var(--fs-sm);
		margin: 0;
	}

	.export-form {
		text-align: center;
		margin-top: var(--sp-4);
	}

	.stat-card {
		padding: var(--sp-3) 0;
		border-bottom: 1px solid var(--c-hairline);
	}

	.stat-card:last-child {
		border-bottom: none;
	}

	.stat-label {
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
		display: block;
		margin-bottom: var(--sp-1);
	}

	.stat-value {
		font-size: var(--fs-body);
		color: var(--c-text-1);
	}

	.stat-list {
		margin: var(--sp-2) 0 0;
		padding-left: var(--sp-6);
		font-size: var(--fs-sm);
		color: var(--c-text-2);
	}

	.stat-list li {
		padding: var(--sp-1) 0;
	}

	.logout-section {
		text-align: center;
		border: none;
		padding: 0;
	}

	.logout-btn {
		padding: var(--sp-2) var(--sp-6);
		font-size: var(--fs-sm);
	}

	.logout-btn:hover {
		border-color: var(--c-error);
		color: var(--c-error);
	}
</style>
