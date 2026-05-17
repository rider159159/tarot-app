<script lang="ts">
	import type { SpreadType } from '$lib/types';
	import { spreadConfigs } from '$lib/tarot';

	const CUSTOM_MIN = 1;
	const CUSTOM_MAX = 10;

	let {
		selected = $bindable(),
		customCardCount = $bindable(3),
		disabled = false
	}: {
		selected: SpreadType;
		customCardCount?: number;
		disabled?: boolean;
	} = $props();

	const options = Object.values(spreadConfigs);

	// Keep the typed card count within 1–10 once the user leaves the field.
	function clampCustomCount() {
		const n = Math.round(Number(customCardCount));
		customCardCount = Number.isFinite(n)
			? Math.min(CUSTOM_MAX, Math.max(CUSTOM_MIN, n))
			: CUSTOM_MIN;
	}
</script>

<fieldset {disabled}>
	<legend>選擇牌陣</legend>
	{#each options as config}
		<label class="spread-option">
			<input type="radio" name="spread" value={config.type} bind:group={selected} />
			<span class="spread-name">{config.name}（{config.cardCount} 張）</span>
			<small class="spread-desc">{config.description}</small>
		</label>
	{/each}

	<label class="spread-option">
		<input type="radio" name="spread" value="custom" bind:group={selected} />
		<span class="spread-name">自定義牌陣</span>
		<small class="spread-desc">
			自選抽牌張數（{CUSTOM_MIN}～{CUSTOM_MAX} 張），每張牌的意義由你自己詮釋。
		</small>
	</label>

	{#if selected === 'custom'}
		<div class="custom-count">
			<label for="custom-card-count">抽牌張數</label>
			<input
				id="custom-card-count"
				type="number"
				min={CUSTOM_MIN}
				max={CUSTOM_MAX}
				step="1"
				bind:value={customCardCount}
				onblur={clampCustomCount}
			/>
			<span class="custom-count-hint">張（{CUSTOM_MIN}～{CUSTOM_MAX}）</span>
		</div>
	{/if}
</fieldset>

<style>
	fieldset {
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		padding: var(--sp-4);
		margin-bottom: var(--sp-4);
	}

	legend {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
		padding: 0 var(--sp-2);
	}

	.spread-option {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--sp-2);
		padding: var(--sp-2) 0;
		cursor: pointer;
	}

	.spread-option input[type='radio'] {
		accent-color: var(--c-accent);
	}

	.spread-name {
		color: var(--c-text-1);
	}

	.spread-desc {
		width: 100%;
		padding-left: var(--sp-6);
		color: var(--c-text-3);
		font-size: var(--fs-sm);
	}

	.custom-count {
		display: flex;
		align-items: center;
		gap: var(--sp-3);
		padding: var(--sp-2) 0 var(--sp-2) var(--sp-6);
	}

	.custom-count label {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
	}

	.custom-count input {
		width: 4.5rem;
		padding: var(--sp-2);
		font-family: inherit;
		font-size: var(--fs-body);
	}

	.custom-count-hint {
		color: var(--c-text-3);
		font-size: var(--fs-sm);
	}
</style>
