<script lang="ts">
	import { page } from '$app/stores';
	import { SITE } from '$lib/seo/config';

	let {
		/** 頁面標題（不含站名後綴）。省略時使用全站預設標題 */
		title = undefined,
		/** 頁面描述 */
		description = SITE.defaultDescription,
		/** 分享圖網址（可為相對路徑） */
		image = SITE.defaultImage,
		/** 禁止搜尋引擎索引此頁（登入後 / 隱私頁面用） */
		noindex = false,
		/** og:type，文章頁可傳 'article' */
		type = 'website'
	}: {
		title?: string;
		description?: string;
		image?: string;
		noindex?: boolean;
		type?: string;
	} = $props();

	// 完整標題：有指定 title 則加站名後綴，否則用預設標題
	let fullTitle = $derived(title ? `${title} — ${SITE.name}` : SITE.defaultTitle);
	// canonical：以正式站網址 + 當前路徑（去除查詢字串）組成
	let canonical = $derived(`${SITE.url}${$page.url.pathname}`);
	// 分享圖補成絕對網址
	let absoluteImage = $derived(image.startsWith('http') ? image : `${SITE.url}${image}`);
</script>

<svelte:head>
	<title>{fullTitle}</title>
	<meta name="description" content={description} />
	<link rel="canonical" href={canonical} />

	{#if noindex}
		<meta name="robots" content="noindex, nofollow" />
	{:else}
		<meta name="robots" content="index, follow" />
	{/if}

	<!-- Open Graph -->
	<meta property="og:type" content={type} />
	<meta property="og:site_name" content={SITE.name} />
	<meta property="og:title" content={fullTitle} />
	<meta property="og:description" content={description} />
	<meta property="og:url" content={canonical} />
	<meta property="og:image" content={absoluteImage} />
	<meta property="og:locale" content={SITE.locale} />

	<!-- Twitter Card -->
	<meta name="twitter:card" content="summary_large_image" />
	<meta name="twitter:title" content={fullTitle} />
	<meta name="twitter:description" content={description} />
	<meta name="twitter:image" content={absoluteImage} />
</svelte:head>
