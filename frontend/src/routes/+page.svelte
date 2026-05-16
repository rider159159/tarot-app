<script lang="ts">
	import { browser } from '$app/environment';
	import { enhance } from '$app/forms';
	import type { SpreadType, ReadingResult, ApiReadingResponse } from '$lib/types';
	import { mapApiResponse } from '$lib/utils/reading';
	import { copyJsonToClipboard, type CopyStatus } from '$lib/utils/clipboard';
	import {
		savePending,
		loadPending,
		clearPending,
		type PendingReading
	} from '$lib/utils/pendingReading';
	import SpreadSelector from '$lib/components/SpreadSelector.svelte';
	import QuestionInput from '$lib/components/QuestionInput.svelte';
	import DrawButton from '$lib/components/DrawButton.svelte';
	import ReadingDisplay from '$lib/components/ReadingDisplay.svelte';
	import CardPicker from '$lib/components/CardPicker.svelte';
	import AnonymousCta from '$lib/components/AnonymousCta.svelte';
	import SavePendingReadingDialog from '$lib/components/SavePendingReadingDialog.svelte';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	// 抽牌模式：'qa' 一般問答（直接顯示結果）/ 'interactive' 模擬抽牌（互動選牌）
	let drawMode: 'qa' | 'interactive' = $state('qa');
	// 模擬抽牌階段：'idle' 尚未抽 / 'picking' 互動選牌中（牌已抽好、等使用者翻）
	let interactivePhase: 'idle' | 'picking' = $state('idle');

	let selectedSpread: SpreadType = $state('single');
	let question: string = $state('');
	let loading: boolean = $state(false);
	let reading: ReadingResult | null = $state(null);

	let copyStatus: CopyStatus = $state('idle');
	let pendingForDialog: PendingReading | null = $state(null);
	let showSaveDialog: boolean = $state(false);
	let toast: { kind: 'success' | 'error'; text: string } | null = $state(null);
	let toastTimer: ReturnType<typeof setTimeout> | null = null;

	function showToast(kind: 'success' | 'error', text: string, ms = 3000) {
		if (toastTimer) clearTimeout(toastTimer);
		toast = { kind, text };
		toastTimer = setTimeout(() => (toast = null), ms);
	}

	// 模擬抽牌：表單回傳的牌結果先暫存於此（不直接顯示），
	// 待使用者在牌海翻完全部卡片，才正式設給 reading。
	let pendingReadingResult: ReadingResult | null = $state(null);

	// 把 draw action 的回傳轉成 ReadingResult；未登入時順便寫 localStorage 暫存。
	// 回傳 null 表示這次 form 沒有抽牌結果（可能是其他 action 或錯誤）。
	function readDrawResult(): ReadingResult | null {
		if (form?.reading) {
			return mapApiResponse(form.reading);
		}
		if (form?.anonymousReading && form?.clientToken) {
			const ar = form.anonymousReading;
			savePending({
				clientToken: form.clientToken,
				spreadType: ar.spreadType,
				question: ar.question ?? null,
				drawnAt: ar.drawnAt,
				cards: ar.cards
			});
			const synthetic: ApiReadingResponse = {
				id: '',
				spreadType: ar.spreadType,
				question: ar.question,
				cards: ar.cards,
				createdAt: ar.drawnAt
			};
			return mapApiResponse(synthetic);
		}
		return null;
	}

	// draw 表單送出後呼叫：依「送出當下」的模式決定直接顯示或進互動選牌。
	// 不放 $effect — $effect 的依賴集合會隨條件分支變動，曾導致模式判斷失準。
	function onDrawResult() {
		const result = readDrawResult();
		if (!result) return;
		if (drawMode === 'interactive') {
			pendingReadingResult = result;
			interactivePhase = 'picking';
		} else {
			reading = result;
		}
	}

	// 3. Import succeeded
	$effect(() => {
		if (form?.importSuccess && form?.importedReading) {
			clearPending();
			pendingForDialog = null;
			showSaveDialog = false;
			reading = mapApiResponse(form.importedReading);
			showToast('success', '已保存到歷史紀錄');
		}
	});

	// 4. Import failed
	$effect(() => {
		if (form?.importError) {
			showToast('error', form.importError, 4000);
		}
	});

	// 5. On mount when logged-in: prompt to save pending
	$effect(() => {
		if (browser && data.loggedIn && !showSaveDialog && !pendingForDialog) {
			const p = loadPending();
			if (p) {
				pendingForDialog = p;
				showSaveDialog = true;
			}
		}
	});

	// 6. Existing export-single effect
	$effect(() => {
		if (form?.exportSuccess && form.exportJson && form.exportedId) {
			copyStatus = 'copying';
			copyJsonToClipboard(form.exportJson).then((ok) => {
				copyStatus = ok ? 'done' : 'error';
				setTimeout(
					() => {
						copyStatus = 'idle';
					},
					ok ? 2000 : 3000
				);
			});
		}
	});

	function handleDrawAgain() {
		reading = null;
		copyStatus = 'idle';
		pendingReadingResult = null;
		interactivePhase = 'idle';
	}

	// 模擬抽牌：牌海翻完全部卡片 → 正式呈現結果。
	// 留一點時間讓最後一張翻牌動畫播完再切到 ReadingDisplay。
	function handlePickComplete() {
		setTimeout(() => {
			if (pendingReadingResult) reading = pendingReadingResult;
			interactivePhase = 'idle';
		}, 700);
	}

	// 切換抽牌模式：清掉進行中的互動狀態，避免殘留
	function switchMode(mode: 'qa' | 'interactive') {
		if (mode === drawMode) return;
		drawMode = mode;
		interactivePhase = 'idle';
		pendingReadingResult = null;
	}

	function getSpreadTypeForApi(spread: SpreadType): string {
		if (spread === 'three-card-time') return 'ThreeCardTime';
		if (spread === 'three-card-problem') return 'ThreeCardProblem';
		if (spread === 'three-card-linear') return 'ThreeCardLinear';
		if (spread === 'celtic-cross') return 'CelticCross';
		return 'Single';
	}

	function handleDismissDialog() {
		clearPending();
		pendingForDialog = null;
		showSaveDialog = false;
	}

	function handleSavedDialog() {
		clearPending();
		pendingForDialog = null;
		showSaveDialog = false;
	}
</script>

<svelte:head>
	<title>塔羅占卜</title>
</svelte:head>

<main>
	<h1>塔羅占卜</h1>

	{#if !reading && interactivePhase === 'idle'}
		<!-- 抽牌模式切換：一般問答（預設）/ 模擬抽牌 -->
		<div class="mode-tabs" role="tablist" aria-label="抽牌模式">
			<button
				type="button"
				role="tab"
				aria-selected={drawMode === 'qa'}
				class="mode-tab"
				class:active={drawMode === 'qa'}
				disabled={loading}
				onclick={() => switchMode('qa')}
			>
				一般問答
			</button>
			<button
				type="button"
				role="tab"
				aria-selected={drawMode === 'interactive'}
				class="mode-tab"
				class:active={drawMode === 'interactive'}
				disabled={loading}
				onclick={() => switchMode('interactive')}
			>
				模擬抽牌
			</button>
		</div>

		<form
			method="POST"
			action="?/draw"
			use:enhance={() => {
				loading = true;
				return async ({ update }) => {
					loading = false;
					await update();
					// update() 後 form 已是最新 — 依當下模式決定顯示或進選牌
					onDrawResult();
				};
			}}
		>
			<input type="hidden" name="spreadType" value={getSpreadTypeForApi(selectedSpread)} />
			<input type="hidden" name="question" value={question} />

			<SpreadSelector bind:selected={selectedSpread} disabled={loading} />
			<QuestionInput bind:value={question} disabled={loading} />
			<DrawButton {loading} />
		</form>

		{#if form?.error}
			<p class="error">{form.error}</p>
		{/if}
	{:else if interactivePhase === 'picking' && pendingReadingResult}
		<!-- 模擬抽牌：牌已抽好，使用者從牌海點選翻牌。
		     需點選張數 = 後端實際抽到的張數，不靠牌陣查表。 -->
		<p class="picker-hint">
			憑直覺從牌海中點選 {pendingReadingResult.cards.length} 張
		</p>
		<CardPicker
			needed={pendingReadingResult.cards.length}
			results={pendingReadingResult.cards}
			oncomplete={handlePickComplete}
		/>
	{:else if reading}
		<ReadingDisplay {reading} />
		<div class="actions">
			<button class="action-btn" onclick={handleDrawAgain}>再抽一次</button>
			{#if reading.id}
				<form method="POST" action="?/exportSingle" use:enhance>
					<input type="hidden" name="id" value={reading.id} />
					<button
						type="submit"
						class="action-btn secondary"
						disabled={copyStatus === 'copying'}
					>
						{#if copyStatus === 'done'}✓ 已複製
						{:else if copyStatus === 'error'}✗ 複製失敗
						{:else if copyStatus === 'copying'}複製中...
						{:else}複製給 AI{/if}
					</button>
				</form>
			{/if}
			{#if data.loggedIn}
				<a href="/history" class="action-btn secondary">查看歷史</a>
			{/if}
		</div>
		{#if form?.exportError}
			<p class="error">{form.exportError}</p>
		{/if}

		{#if !data.loggedIn}
			<AnonymousCta returnTo="/" />
		{/if}
	{/if}

	{#if toast}
		<p class="toast {toast.kind}">{toast.text}</p>
	{/if}

	{#if showSaveDialog && pendingForDialog}
		<SavePendingReadingDialog
			pending={pendingForDialog}
			onDismiss={handleDismissDialog}
			onSaved={handleSavedDialog}
		/>
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
		margin-top: var(--sp-4);
	}

	/* 模式切換 tab：兩段式，底線標示 active */
	.mode-tabs {
		display: flex;
		gap: var(--sp-2);
		justify-content: center;
		margin-bottom: var(--sp-8);
		border-bottom: 1px solid var(--c-hairline);
	}

	.mode-tab {
		padding: var(--sp-3) var(--sp-6);
		background: transparent;
		border: none;
		border-bottom: 2px solid transparent;
		color: var(--c-text-3);
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		margin-bottom: -1px;
		transition: color var(--transition), border-color var(--transition);
	}

	.mode-tab:hover:not(:disabled) {
		color: var(--c-text-1);
	}

	.mode-tab.active {
		color: var(--c-accent);
		border-bottom-color: var(--c-accent);
	}

	.mode-tab:disabled {
		cursor: default;
		opacity: 0.5;
	}

	.picker-hint {
		text-align: center;
		color: var(--c-text-2);
		font-size: var(--fs-sm);
		margin: 0 0 var(--sp-6);
	}

	.actions {
		display: flex;
		justify-content: center;
		gap: var(--sp-4);
		margin-top: var(--sp-6);
		flex-wrap: wrap;
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.action-btn {
		padding: var(--sp-2) var(--sp-6);
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		background: transparent;
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		text-decoration: none;
		transition: border-color var(--transition), color var(--transition);
	}

	.action-btn:hover {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.toast {
		text-align: center;
		margin: var(--sp-4) auto 0;
		padding: var(--sp-3) var(--sp-4);
		border-radius: var(--radius-0);
		max-width: 32rem;
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
	}

	.toast.success {
		background: var(--c-success-bg);
		color: var(--c-success);
		border: 1px solid var(--c-success);
	}

	.toast.error {
		background: var(--c-error-bg);
		color: var(--c-error);
		border: 1px solid var(--c-error);
	}
</style>
