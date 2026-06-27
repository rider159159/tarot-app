<script lang="ts">
	import Seo from '$lib/components/Seo.svelte';
</script>

<Seo title="驗證中" description="正在處理你的帳號驗證。" noindex />

<main>
	<p>正在完成驗證，請稍候…</p>
</main>

<style>
	main {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 100vh;
		color: var(--c-text-3);
	}
</style>
