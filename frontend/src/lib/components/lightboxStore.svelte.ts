// Shared lightbox state — a single module-level store so any card image can
// open the lightbox without prop-drilling. The <Lightbox> component is mounted
// once in +layout.svelte and reads this state.

interface LightboxImage {
	src: string;
	alt: string;
	/** Whether the card is reversed — the lightbox rotates the image 180°. */
	reversed: boolean;
}

let current = $state<LightboxImage | null>(null);

export const lightbox = {
	get image() {
		return current;
	},
	open(src: string, alt: string, reversed = false) {
		current = { src, alt, reversed };
	},
	close() {
		current = null;
	}
};
