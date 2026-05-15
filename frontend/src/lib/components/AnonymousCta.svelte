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
	<p class="hint">這次抽牌乽暫存在瀙覽器，登入後會問你要不要保存</p>
</aside>

<style>
	.cta {
		margin: 2rem 0 0;
		padding: 1.25rem 1.5rem;
		background: #f5f0fa;
		border: 1px solid #d8c8e8;
		border-radius: 8px;
		text-align: left;
	}

	h3 {
		margin: 0 0 0.75rem;
		font-size: 1rem;
		color: #4a3060;
	}

	ul {
		margin: 0 0 1rem;
		padding-left: 1.25rem;
		color: #555;
		font-size: 0.9rem;
	}

	ul li {
		margin-bottom: 0.25rem;
	}

	.buttons {
		display: flex;
		gap: 0.75rem;
		flex-wrap: wrap;
	}

	.btn {
		display: inline-block;
		padding: 0.5rem 1.25rem;
		border-radius: 6px;
		text-decoration: none;
		font-size: 0.9rem;
		border: 1px solid #4a3060;
		color: #4a3060;
		background: #fff;
	}

	.btn.primary {
		background: #4a3060;
		color: #fff;
	}

	.btn:hover {
		filter: brightness(1.05);
	}

	.hint {
		margin: 0.75rem 0 0;
		font-size: 0.8rem;
		color: #888;
	}
</style>
