<script lang="ts">
	let {
		value = $bindable(),
		disabled = false
	}: {
		value: string;
		disabled?: boolean;
	} = $props();
</script>

<div class="question-input">
	<label for="question">你的問題（選填）</label>
	<textarea
		id="question"
		bind:value
		{disabled}
		placeholder="輸入你想問的問題..."
		rows="3"
	></textarea>
</div>

<style>
	.question-input {
		margin-bottom: var(--sp-4);
	}

	label {
		display: block;
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
		margin-bottom: var(--sp-2);
	}

	textarea {
		width: 100%;
		padding: var(--sp-3);
		font-family: inherit;
		font-size: var(--fs-body);
		line-height: var(--lh-body);
		resize: vertical;
	}
</style>
