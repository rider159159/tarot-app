import { SITE } from '$lib/seo/config';
import type { RequestHandler } from './$types';

/** 動態產生 robots.txt：開放公開頁面、阻擋登入後的隱私頁面 */
export const GET: RequestHandler = () => {
	const body = `User-agent: *
Allow: /
Disallow: /profile
Disallow: /history
Disallow: /login
Disallow: /register
Disallow: /forgot-password
Disallow: /auth/

Sitemap: ${SITE.url}/sitemap.xml
`;

	return new Response(body, {
		headers: {
			'Content-Type': 'text/plain',
			'Cache-Control': 'public, max-age=86400'
		}
	});
};
