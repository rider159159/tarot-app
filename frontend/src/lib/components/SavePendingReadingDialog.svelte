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
		background: rgba(14, 10, 20, 0.8);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
		padding: var(--sp-4);
	}

	.dialog {
		background: var(--c-surface-1);
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-0);
		padding: var(--sp-6);
		max-width: 420px;
		width: 100%;
	}

	h2 {
		margin: 0 0 var(--sp-2);
		font-size: var(--fs-h3);
		color: var(--c-text-1);
	}

	p {
		margin: 0 0 var(--sp-6);
		color: var(--c-text-2);
		font-size: var(--fs-sm);
	}

	.actions {
		display: flex;
		gap: var(--sp-3);
		justify-content: flex-end;
	}

	.actions form {
		margin: 0;
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.btn {
		padding: var(--sp-2) var(--sp-6);
		border-radius: var(--radius-pill);
		border: 1px solid var(--c-hairline-strong);
		background: transparent;
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		cursor: pointer;
		transition: border-color var(--transition), color var(--transition);
	}

	.btn:hover:not(:disabled) {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.btn:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}
</style>
