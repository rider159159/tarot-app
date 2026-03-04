// Static adapter config for Capacitor iOS builds
// Usage: SVELTE_KIT_CONFIG=svelte.config.static.js pnpm build
// Then: npx cap sync ios

import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			fallback: '200.html'
		})
	}
};

export default config;
