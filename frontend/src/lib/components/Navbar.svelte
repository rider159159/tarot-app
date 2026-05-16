<script lang="ts">
	import { page } from '$app/stores';

	let currentPath = $derived($page.url.pathname);
	let user = $derived($page.data.user);
	let displayName = $derived(user?.user_metadata?.display_name ?? user?.email ?? '');

	// Anonymous users only see the draw page link.
	// Logged-in users get the full nav.
	let navItems = $derived(
		user
			? [
					{ href: '/', label: '抽牌' },
					{ href: '/history', label: '歷史紀錄' },
					{ href: '/profile', label: '個人頁面' }
			  ]
			: [{ href: '/', label: '抽牌' }]
	);
</script>

<nav>
	<div class="nav-inner">
		<ul class="nav-links">
			{#each navItems as item}
				<li>
					<a href={item.href} class:active={currentPath === item.href}>
						{item.label}
					</a>
				</li>
			{/each}
		</ul>
		{#if displayName}
			<div class="user-area">
				<span class="user-name">{displayName}</span>
				<form method="POST" action="/auth/logout">
					<button type="submit" class="logout-btn">登出</button>
				</form>
			</div>
		{:else}
			<div class="user-area">
				<a class="auth-link" href="/login">登入</a>
				<a class="auth-link primary" href="/register">註冊</a>
			</div>
		{/if}
	</div>
</nav>

<style>
	nav {
		background: var(--c-surface-1);
		border-bottom: 1px solid var(--c-hairline);
		padding: 0 var(--sp-4);
	}

	.nav-inner {
		max-width: var(--content-max);
		margin: 0 auto;
		display: flex;
		align-items: center;
		justify-content: space-between;
		height: 3.5rem;
	}

	.nav-links {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		gap: var(--sp-2);
	}

	.nav-links a {
		color: var(--c-text-3);
		text-decoration: none;
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		padding: var(--sp-2) var(--sp-3);
		border-bottom: 1px solid transparent;
		transition: color var(--transition), border-color var(--transition);
	}

	.nav-links a:hover {
		color: var(--c-text-1);
	}

	/* active 用月光色文字 + 底部 hairline，不用背景塊 */
	.nav-links a.active {
		color: var(--c-accent);
		border-bottom-color: var(--c-accent);
	}

	.user-area {
		display: flex;
		align-items: center;
		gap: var(--sp-3);
	}

	.user-name {
		color: var(--c-text-3);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
	}

	/* 按鈕樣板：透明底 + 1px 外框 + pill */
	.logout-btn,
	.auth-link {
		background: transparent;
		border: 1px solid var(--c-hairline-strong);
		border-radius: var(--radius-pill);
		color: var(--c-text-1);
		font-family: var(--font-mono);
		font-size: var(--fs-xs);
		letter-spacing: var(--ls-mono);
		padding: var(--sp-1) var(--sp-3);
		text-decoration: none;
		transition: border-color var(--transition), color var(--transition);
	}

	.logout-btn:hover,
	.auth-link:hover {
		border-color: var(--c-accent);
		color: var(--c-accent);
	}
</style>
