<script lang="ts">
	import { enhance } from '$app/forms';
	import type { SpreadType, ReadingResult } from '$lib/types';
	import { mapApiResponse } from '$lib/utils/reading';
	import { copyJsonToClipboard, type CopyStatus } from '$lib/utils/clipboard';
	import SpreadSelector from '$lib/components/SpreadSelector.svelte';
	import QuestionInput from '$lib/components/QuestionInput.svelte';
	import DrawButton from '$lib/components/DrawButton.svelte';
	import ReadingDisplay from '$lib/components/ReadingDisplay.svelte';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();

	let selectedSpread: SpreadType = $state('single');
	let question: string = $state('');
	let loading: boolean = $state(false);
	let reading: ReadingResult | null = $state(null);
	let copyStatus: CopyStatus = $state('idle');

	$effect(() => {
		if (form?.reading) {
			reading = mapApiResponse(form.reading);
		}
	});

	$effect(() => {
		if (form?.exportSuccess && form.exportJson && form.exportedId) {
			copyStatus = 'copying';
			copyJsonToClipboard(form.exportJson).then((ok) => {
				copyStatus = ok ? 'done' : 'error';
				setTimeout(() => {
					copyStatus = 'idle';
				}, ok ? 2000 : 3000);
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
			<a href="/history" class="action-btn secondary">查看歷史</a>
		</div>
		{#if form?.exportError}
			<p class="error">{form.exportError}</p>
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
		margin-top: 1rem;
	}

	.actions {
		display: flex;
		justify-content: center;
		gap: 1rem;
		margin-top: 1.5rem;
	}

	.action-btn {
		padding: 0.5rem 1.25rem;
		border: 1px solid #4a3060;
		border-radius: 6px;
		background: #4a3060;
		color: #fff;
		font-size: 0.9rem;
		cursor: pointer;
		font-family: inherit;
		text-decoration: none;
	}

	.action-btn:hover {
		background: #3a2050;
	}

	.action-btn.secondary {
		background: none;
		color: #4a3060;
	}

	.action-btn.secondary:hover {
		background: #f0ecf5;
	}
</style>
