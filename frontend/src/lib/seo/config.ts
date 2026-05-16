/** 全站 SEO 設定 */
export const SITE = {
	/** 正式站網址（無結尾斜線） */
	url: 'https://rtarot.zeabur.app',
	/** 站名，用於 title 後綴與 og:site_name */
	name: '塔羅占卜',
	/** 預設標題（首頁與無指定 title 時使用） */
	defaultTitle: '塔羅占卜 — 線上免費塔羅牌占卜',
	/** 預設描述 */
	defaultDescription:
		'免費線上塔羅牌占卜，提供單張、時間之流、聖三角與凱爾特十字等多種牌陣。輸入你的問題，抽牌獲得指引，並可登入保存占卜歷史。',
	/** 預設分享圖（相對於站台根目錄，建議 1200x630） */
	defaultImage: '/og-image.png',
	/** 語系 */
	locale: 'zh_TW',
	/** 主題色（瀏覽器 UI） */
	themeColor: '#1a1626'
} as const;
