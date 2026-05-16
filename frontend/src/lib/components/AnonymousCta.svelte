<script lang="ts">
	let { returnTo = '/' }: { returnTo?: string } = $props();
	let encoded = $derived(encodeURIComponent(returnTo));
</script>

<aside class="cta" aria-label="登入註冊推薦">
	<h3>登入後可以更進一步</h3>
	<ul>
		<li><strong>保存</strong>這次抽牌與未來所有紀錄</li>
		<li><strong>查看</strong>個人塔羅統計與出現最多的牌</li>
		<li><strong>每週</strong>一次的本週週運專屬抽牌</li>
	</ul>
	<div class="buttons">
		<a href={`/login?returnTo=${encoded}`} class="btn primary">登入</a>
		<a href={`/register?returnTo=${encoded}`} class="btn">註冊</a>
	</div>
	<p class="hint">這次抽牌會暫存在瀏覽器，登入後會問你要不要保存</p>
</aside>

<style>
	.cta {
		margin: var(--sp-8) 0 0;
		padding: var(--sp-6);
		background: var(--c-surface-1);
		border: 1px solid var(--c-hairline);
		border-radius: var(--radius-0);
		text-align: left;
	}

	h3 {
		margin: 0 0 var(--sp-3);
		font-size: var(--fs-body);
		color: var(--c-text-1);
	}

	ul {
		margin: 0 0 var(--sp-4);
		padding-left: var(--sp-6);
		color: var(--c-text-2);
		font-size: var(--fs-sm);
	}

	ul li {
		margin-bottom: var(--sp-1);
	}

	ul strong {
		font-weight: var(--fw);
		color: var(--c-accent);
	}

	.buttons {
		display: flex;
		gap: var(--sp-3);
		flex-wrap: wrap;
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.btn {
		display: inline-block;
		padding: var(--sp-2) var(--sp-6);
		border-radius: var(--radius-pill);
		text-decoration: none;
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		border: 1px solid var(--c-hairline-strong);
		color: var(--c-text-1);
		background: transparent;
		transition: border-color var(--transition), color var(--transition);
	}

	.btn:hover {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}

	.hint {
		margin: var(--sp-3) 0 0;
		font-size: var(--fs-xs);
		color: var(--c-text-3);
	}
</style>
