<script lang="ts">
	import { page } from '$app/stores';
	import Navbar from '$lib/components/Navbar.svelte';

	let { children } = $props();

	const publicPaths = ['/login', '/register', '/auth/callback'];
	let showNavbar = $derived(!publicPaths.includes($page.url.pathname));
</script>

{#if showNavbar}
	<Navbar />
{/if}

{@render children()}
