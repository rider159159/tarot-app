<script lang="ts">
	import type { SpreadType } from '$lib/types';
	import { spreadConfigs } from '$lib/tarot';

	let {
		selected = $bindable(),
		disabled = false
	}: {
		selected: SpreadType;
		disabled?: boolean;
	} = $props();

	const options = Object.values(spreadConfigs);
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
</style>
