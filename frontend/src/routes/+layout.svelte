<script lang="ts">
	import '$lib/styles/base.css';
	import { page } from '$app/stores';
	import Navbar from '$lib/components/Navbar.svelte';
	import Lightbox from '$lib/components/Lightbox.svelte';

	let { children } = $props();

	const publicPaths = ['/login', '/register', '/auth/callback'];
	let showNavbar = $derived(!publicPaths.includes($page.url.pathname));
</script>

{#if showNavbar}
	<Navbar />
{/if}

{@render children()}

<Lightbox />
