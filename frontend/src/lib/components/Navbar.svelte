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
		background: #4a3060;
		padding: 0 1rem;
		font-family: system-ui, -apple-system, sans-serif;
	}

	.nav-inner {
		max-width: 800px;
		margin: 0 auto;
		display: flex;
		align-items: center;
		justify-content: space-between;
		height: 3rem;
	}

	.nav-links {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		gap: 0.25rem;
	}

	.nav-links a {
		color: rgba(255, 255, 255, 0.75);
		text-decoration: none;
		font-size: 0.9rem;
		padding: 0.5rem 0.75rem;
		border-radius: 6px;
		transition: background 0.15s, color 0.15s;
	}

	.nav-links a:hover {
		color: #fff;
		background: rgba(255, 255, 255, 0.1);
	}

	.nav-links a.active {
		color: #fff;
		background: rgba(255, 255, 255, 0.15);
	}

	.user-area {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.user-name {
		color: rgba(255, 255, 255, 0.8);
		font-size: 0.85rem;
	}

	.logout-btn {
		background: none;
		border: 1px solid rgba(255, 255, 255, 0.3);
		color: rgba(255, 255, 255, 0.75);
		font-size: 0.8rem;
		padding: 0.25rem 0.6rem;
		border-radius: 4px;
		cursor: pointer;
		font-family: inherit;
		transition: background 0.15s, color 0.15s;
	}

	.logout-btn:hover {
		background: rgba(255, 255, 255, 0.1);
		color: #fff;
	}

	.auth-link {
		color: rgba(255, 255, 255, 0.85);
		text-decoration: none;
		font-size: 0.85rem;
		padding: 0.3rem 0.7rem;
		border-radius: 4px;
		border: 1px solid rgba(255, 255, 255, 0.3);
		transition: background 0.15s, color 0.15s;
	}

	.auth-link:hover {
		background: rgba(255, 255, 255, 0.1);
		color: #fff;
	}

	.auth-link.primary {
		background: rgba(255, 255, 255, 0.15);
	}
</style>
