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
		margin-bottom: 1rem;
	}

	label {
		display: block;
		font-weight: bold;
		margin-bottom: 0.5rem;
	}

	textarea {
		width: 100%;
		padding: 0.75rem;
		border: 1px solid #ddd;
		border-radius: 8px;
		font-family: inherit;
		font-size: 1rem;
		resize: vertical;
		box-sizing: border-box;
	}

	textarea:focus {
		outline: none;
		border-color: #666;
	}
</style>
