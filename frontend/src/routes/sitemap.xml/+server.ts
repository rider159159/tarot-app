import { SITE } from '$lib/seo/config';
import type { RequestHandler } from './$types';

/** 可被索引的公開頁面（不含登入後 / 隱私頁面） */
const pages: { path: string; priority: string; changefreq: string }[] = [
	{ path: '/', priority: '1.0', changefreq: 'weekly' }
];

/** 動態產生 sitemap.xml */
export const GET: RequestHandler = () => {
	const today = new Date().toISOString().split('T')[0];

	const urls = pages
		.map(
			(p) => `	<url>
		<loc>${SITE.url}${p.path}</loc>
		<lastmod>${today}</lastmod>
		<changefreq>${p.changefreq}</changefreq>
		<priority>${p.priority}</priority>
	</url>`
		)
		.join('\n');

	const body = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${urls}
</urlset>`;

	return new Response(body, {
		headers: {
			'Content-Type': 'application/xml',
			'Cache-Control': 'public, max-age=86400'
		}
	});
};
