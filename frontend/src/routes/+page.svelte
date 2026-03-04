<script lang="ts">
	import { enhance } from '$app/forms';
	import type { SpreadType, ReadingResult } from '$lib/types';
	import { mapApiResponse } from '$lib/utils/reading';
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

	$effect(() => {
		if (form?.reading) {
			reading = mapApiResponse(form.reading);
		}
	});

	function handleDrawAgain() {
		reading = null;
	}

	function getSpreadTypeForApi(spread: SpreadType): string {
		if (spread === 'three-card') return 'ThreeCard';
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
			<a href="/history" class="action-btn secondary">查看歷史</a>
		</div>
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
