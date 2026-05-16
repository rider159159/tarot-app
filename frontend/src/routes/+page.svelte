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
	import AnonymousCta from '$lib/components/AnonymousCta.svelte';
	import SavePendingReadingDialog from '$lib/components/SavePendingReadingDialog.svelte';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

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

	// 1. Logged-in draw response
	$effect(() => {
		if (form?.reading) {
			reading = mapApiResponse(form.reading);
		}
	});

	// 2. Anonymous draw → persist to localStorage + render synthetic result
	$effect(() => {
		if (form?.anonymousReading && form?.clientToken) {
			const ar = form.anonymousReading;
			const pending: PendingReading = {
				clientToken: form.clientToken,
				spreadType: ar.spreadType,
				question: ar.question ?? null,
				drawnAt: ar.drawnAt,
				cards: ar.cards
			};
			savePending(pending);
			// Build an ApiReadingResponse-shaped object so mapApiResponse works.
			const synthetic: ApiReadingResponse = {
				id: '',
				spreadType: ar.spreadType,
				question: ar.question,
				cards: ar.cards,
				createdAt: ar.drawnAt
			};
			reading = mapApiResponse(synthetic);
		}
	});

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

	{#if !reading}
		<form
			method="POST"
			action="?/draw"
			use:enhance={() => {
				loading = true;
				return async ({ update }) => {
					loading = false;
					await update();
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
	{:else}
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
