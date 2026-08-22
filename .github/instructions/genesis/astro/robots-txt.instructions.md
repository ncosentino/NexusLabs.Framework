---
applyTo: "**/robots.txt"
---

# robots.txt Rules

## Purpose

`robots.txt` controls which crawlers can access which parts of the site. It lives in the
`public/` directory and is served at the site root.

## Required entries

Every `robots.txt` must include:

```
User-agent: *
Allow: /

Sitemap: https://yourdomain.com/sitemap-index.xml
```

## Search, answer, and training crawler access

The default may favor broad discoverability, but the site owner chooses each purpose
based on privacy, licensing, and content policy. Search/retrieval and model-training
crawlers are independent and should not be treated as one permission.

```
User-agent: OAI-SearchBot
Allow: /

User-agent: ChatGPT-User
Allow: /

User-agent: Claude-SearchBot
Allow: /

User-agent: PerplexityBot
Allow: /

User-agent: GPTBot
Allow: /

User-agent: ClaudeBot
Allow: /

User-agent: Google-Extended
Allow: /

User-agent: Googlebot
Allow: /
```

For example, an owner may allow `OAI-SearchBot` and `Claude-SearchBot` while blocking
`GPTBot`, `ClaudeBot`, or `Google-Extended`. Verify current vendor user-agent names
before changing policy. Robots exclusion is advisory and does not itself grant a
license, establish consent, or guarantee crawler behavior.

## What to disallow

- API endpoints (if any): `Disallow: /api/`
- Admin/internal routes: `Disallow: /admin/`
- Search results pages: `Disallow: /search`

Do NOT disallow CSS, JS, or image paths — search engines need these to render pages properly.

## Sitemap references

List every sitemap the site produces:

```
Sitemap: https://yourdomain.com/sitemap-index.xml
Sitemap: https://yourdomain.com/video-sitemap.xml
```

Use the full absolute URL, not relative paths.

## Domain must match

The sitemap URL domain must match the `site` value in `astro.config.mjs`. Mismatches cause
search engines to ignore the sitemap.
