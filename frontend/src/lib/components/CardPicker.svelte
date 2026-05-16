<script lang="ts">
	// 互動抽牌：78 張牌桂亂撒在一片可捲動的牌海上，使用者點滿 needed 張，點一張翻一張。
	// 翻出的牌依「後端結果順序」對應 — 第 k 次點選拿 results[k]。
	// 牌海上只翻出照片（含正逆位）；完整牌義在選滿後由結果頁的 ReadingDisplay 呈現。
	//
	// 佈局用 DOM 絕對定位（非 canvas）：每張牌的隨機 left/top/rotate 在建立時
	// 算一次並固定，翻牌時不重排，避免牌「跳位」。
	//
	// 牌寬與欄數依容器實際寬度（ResizeObserver）自適應 — 寬螢幕用大牌多欄、
	// 窄螢幕用小牌少欄。位置是 JS 算的，故用 JS 量測而非純 CSS media query。
	import type { DrawnCard } from '$lib/types';
	import FlippableCard from './FlippableCard.svelte';

	let {
		needed = 3,
		results = [],
		oncomplete
	}: {
		needed?: number;
		// 後端已抽好的牌，順序即翻牌揭示順序
		results?: DrawnCard[];
		oncomplete?: () => void;
	} = $props();

	const DECK_SIZE = 78; // 完整一副牌，統一固定
	const CARD_AR = 380 / 220; // 牌高 / 寬（與 card-back.svg 一致）

	function rand(max: number) {
		const a = new Uint32Array(1);
		crypto.getRandomValues(a);
		return (a[0] / 0x100000000) * max;
	}

	// 依容器寬度決定牌寬與概念欄數 — 斷點對應手機 / 平板 / 桌機
	function layoutFor(width: number): { cardW: number; cols: number } {
		if (width < 480) return { cardW: 64, cols: 7 };
		if (width < 900) return { cardW: 84, cols: 9 };
		return { cardW: 104, cols: 11 };
	}

	// 容器寬度量測
	let scrollEl = $state<HTMLDivElement | null>(null);
	let boardWidth = $state(900); // 量測前的合理預設
	$effect(() => {
		if (!scrollEl) return;
		const ro = new ResizeObserver((entries) => {
			boardWidth = entries[0].contentBoxSize[0].inlineSize;
		});
		ro.observe(scrollEl);
		return () => ro.disconnect();
	});

	const layout = $derived(layoutFor(boardWidth));
	const cardW = $derived(layout.cardW);
	const cardH = $derived(cardW * CARD_AR);

	interface Slot {
		id: number;
		left: number; // px
		top: number; // px
		rotate: number; // deg
	}

	// 散亂位置：依目前 cardW / cols 算好並固定，直到佈局尺寸變動才重算
	function buildSlots(cw: number, ch: number, cols: number): Slot[] {
		const cellW = cw * 0.62; // 水平刻意重疊
		const cellH = ch * 0.42; // 垂直刻意重疊
		const out: Slot[] = [];
		for (let i = 0; i < DECK_SIZE; i++) {
			const col = i % cols;
			const row = Math.floor(i / cols);
			out.push({
				id: i,
				left: col * cellW + rand(cellW * 0.9),
				top: row * cellH + rand(cellH * 0.9),
				rotate: rand(44) - 22 // -22°~+22°
			});
		}
		return out;
	}

	let slots = $state<Slot[]>([]);
	$effect(() => {
		// cardW / cardH / cols 變動（裝置寬度改變）即重算散亂佈局
		slots = buildSlots(cardW, cardH, layout.cols);
		pickedOrder = {};
	});

	// 牌海容器尺寸 — 依最末一張牌算出總寬高
	const boardW = $derived(Math.max(0, ...slots.map((s) => s.left)) + cardW + 8);
	const boardH = $derived(Math.max(0, ...slots.map((s) => s.top)) + cardH + 8);

	// slotId → 第幾個被選中（0-based）；未選為 undefined
	let pickedOrder = $state<Record<number, number>>({});
	const pickedCount = $derived(Object.keys(pickedOrder).length);
	const done = $derived(pickedCount >= needed);

	function pick(slotId: number) {
		if (done || pickedOrder[slotId] !== undefined) return;
		const order = pickedCount;
		pickedOrder = { ...pickedOrder, [slotId]: order };

		if (order + 1 >= needed) oncomplete?.();
	}

	function faceFor(slotId: number): DrawnCard | null {
		const order = pickedOrder[slotId];
		return order === undefined ? null : (results[order] ?? null);
	}
</script>

<div class="picker">
	<p class="counter" aria-live="polite">
		已選 <strong>{pickedCount}</strong> / {needed}
		{#if done}<span class="done-tag">— 完成</span>{/if}
	</p>

	<!-- 可捲動牌海：橫向 + 縱向皆可滾，寬度由 ResizeObserver 量測 -->
	<div class="board-scroll" bind:this={scrollEl}>
		<div
			class="board"
			role="group"
			aria-label="牌海，點選 {needed} 張"
			style="width:{boardW}px; height:{boardH}px;"
		>
			{#each slots as slot (slot.id)}
				{@const isPicked = pickedOrder[slot.id] !== undefined}
				<div
					class="slot"
					class:picked={isPicked}
					style="
						left:{slot.left}px;
						top:{slot.top}px;
						width:{cardW}px;
						--rot:{slot.rotate}deg;
						z-index:{isPicked ? 200 + slot.id : slot.id};
					"
				>
					<FlippableCard
						face={faceFor(slot.id)}
						flipped={isPicked}
						selected={isPicked}
						disabled={done}
						onpick={() => pick(slot.id)}
					/>
				</div>
			{/each}
		</div>
	</div>
</div>

<style>
	.counter {
		font-family: var(--font-mono);
		font-size: var(--fs-sm);
		letter-spacing: var(--ls-mono);
		color: var(--c-text-3);
		text-align: center;
		margin: 0 0 var(--sp-4);
	}

	.counter strong {
		color: var(--c-accent);
		font-size: var(--fs-h3);
	}

	.done-tag {
		color: var(--c-success);
	}

	/* 牌海視窗：固定高度、可捲動 */
	.board-scroll {
		max-height: 70vh;
		overflow: auto;
		border: 1px solid var(--c-hairline);
		background: var(--c-surface-1);
		padding: var(--sp-4);
	}

	.board {
		position: relative;
		margin: 0 auto;
	}

	.slot {
		position: absolute;
		transform: rotate(var(--rot));
		transition: transform var(--transition);
	}

	/* hover：牌微微抬起並轉正一點，提示「可點」 */
	.slot:not(.picked):hover {
		transform: rotate(calc(var(--rot) * 0.4)) translateY(-10px) scale(1.04);
		z-index: 300 !important;
	}

	/* 選中：轉正、抬起，浮到最上層（z-index 由 inline style 處理） */
	.slot.picked {
		transform: rotate(0deg) translateY(-6px) scale(1.06);
	}
</style>
