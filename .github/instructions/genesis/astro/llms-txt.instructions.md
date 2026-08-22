---
applyTo: "**/llms.txt"
---

# llms.txt Rules

## Purpose

`llms.txt` is an optional informal proposal for a concise, machine-readable map of a
site. Adoption and vendor consumption are not guaranteed, and it is neither a
`robots.txt` replacement nor an IETF/W3C crawler-control standard.

When selected, it lives in `public/` and is served at the site root. Keep it because it
is low-cost, factual discovery metadata that compatible tools may consume, not because
it has a proven ranking or citation effect.

## Discovery

When the project ships the file, retain its low-cost `<head>` discovery link:

```html
<link rel="alternate" type="text/plain" href="/llms.txt" />
```

This link is a discoverability hint, not proof that a crawler reads the file.

## Required structure

```
# SiteName

> One-line description of the site/organization and what it does.

## Pages

- [Home](https://yourdomain.com): What the homepage covers
- [About](https://yourdomain.com/about): Who runs this, background, mission
- [Services](https://yourdomain.com/services): What services are offered

## Contact

- Email: contact@yourdomain.com
- Location: City, State (if applicable)
- Phone: (if applicable)
```

## Content guidelines

- Be factual and specific; automated tools may quote or summarize this content
- Use the full canonical URL for every link
- Describe each page in one clear sentence
- Include the most important pages — not every page
- Update when pages are added or removed
- Do not include marketing language or superlatives
- Include contact information if publicly available
- Keep crawler permissions in `robots.txt`; `llms.txt` does not grant or deny access

## For content-heavy sites

Add sections for content categories:

```
## Blog

- [Latest posts](https://yourdomain.com/blog): Technology articles and tutorials
- [Category: .NET](https://yourdomain.com/blog/category/dotnet): C# and .NET content

## Videos

- [All videos](https://yourdomain.com/videos): Video content library
```

## Do NOT include

- Secrets, API keys, or internal URLs
- Passwords or authentication endpoints
- Private/draft content URLs
- Personally identifiable information beyond what's publicly available on the site
