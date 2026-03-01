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
		border: 1px solid #ddd;
		border-radius: 8px;
		padding: 1rem;
		margin-bottom: 1rem;
	}

	legend {
		font-weight: bold;
		padding: 0 0.5rem;
	}

	.spread-option {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem;
		padding: 0.5rem 0;
		cursor: pointer;
	}

	.spread-name {
		font-weight: 500;
	}

	.spread-desc {
		width: 100%;
		padding-left: 1.5rem;
		color: #666;
		font-size: 0.875rem;
	}
</style>
