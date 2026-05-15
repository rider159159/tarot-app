<script lang="ts">
	import { enhance } from '$app/forms';
	import type { PendingReading } from '$lib/utils/pendingReading';

	let {
		pending,
		onDismiss,
		onSaved
	}: {
		pending: PendingReading;
		onDismiss: () => void;
		onSaved: () => void;
	} = $props();

	let submitting = $state(false);
</script>

<div class="overlay" role="presentation">
	<div class="dialog" role="dialog" aria-modal="true" aria-labelledby="save-pending-title">
		<h2 id="save-pending-title">保存剛剛的抽牌嗎？</h2>
		<p>你登入前抽了一次牌，還沒進歷史。要存進你的帳號嗎？</p>

		<div class="actions">
			<form
				method="POST"
				action="/?/importPending"
				use:enhance={() => {
					submitting = true;
					return async ({ result, update }) => {
						submitting = false;
						if (result.type === 'success') {
							onSaved();
						}
						await update();
					};
				}}
			>
				<input type="hidden" name="payload" value={JSON.stringify(pending)} />
				<button type="submit" class="btn primary" disabled={submitting}>
					{submitting ? '保存中…' : '保存'}
				</button>
			</form>
			<button type="button" class="btn" onclick={onDismiss} disabled={submitting}>
				不保存
			</button>
		</div>
	</div>
</div>

<style>
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
		padding: 1rem;
	}

	.dialog {
		background: #fff;
		border-radius: 10px;
		padding: 1.5rem;
		max-width: 420px;
		width: 100%;
		box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
	}

	h2 {
		margin: 0 0 0.5rem;
		font-size: 1.1rem;
		color: #4a3060;
	}

	p {
		margin: 0 0 1.25rem;
		color: #555;
		font-size: 0.9rem;
	}

	.actions {
		display: flex;
		gap: 0.75rem;
		justify-content: flex-end;
	}

	.actions form {
		margin: 0;
	}

	.btn {
		padding: 0.5rem 1.25rem;
		border-radius: 6px;
		border: 1px solid #4a3060;
		background: #fff;
		color: #4a3060;
		font-size: 0.9rem;
		cursor: pointer;
		font-family: inherit;
	}

	.btn.primary {
		background: #4a3060;
		color: #fff;
	}

	.btn:disabled {
		opacity: 0.6;
		cursor: not-allowed;
	}
</style>
