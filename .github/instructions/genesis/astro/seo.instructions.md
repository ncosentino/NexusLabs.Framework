---
applyTo: "**/*.astro"
---

# Astro discovery metadata

Use the template's shared layout/site configuration instead of duplicating head
metadata per page.

## Page head

Every indexable page provides:

- one unique, descriptive `<title>`;
- one accurate meta description;
- a canonical URL;
- the intended robots policy;
- Open Graph title, description, type, URL, image, and image alt;
- Twitter card metadata matching the same content.

Content pages include accurate published/modified/author values. Do not fabricate
dates, people, claims, social accounts, or image dimensions.

Content-driven sites expose their existing RSS feed in the head. Do not add an empty
feed to a site without published content.

## Document semantics

- One visible `<h1>` names the page.
- Heading levels stay sequential.
- Links use descriptive text rather than generic calls to click/read.
- Images use descriptive alt text, intrinsic dimensions, responsive sources, and
  lazy loading below the fold.
- Only the actual LCP image is high priority/preloaded.

Long headings, URLs, and project names must wrap without horizontal overflow.

## Structured data

JSON-LD reflects visible page content and uses canonical absolute URLs.

- Site/root pages may use `WebSite` and `Organization`.
- Articles use `Article`/`BlogPosting` with real author/date/image data.
- Hierarchical pages use `BreadcrumbList`.
- FAQ, video, service, product, person, or local-business schema is emitted only when
  the page visibly contains that content.

Validate generated JSON and keep structured data synchronized with rendered content.
Schema.org validity and a consumer's presentation eligibility are separate:

- Keep accurate, low-cost schema without promising a ranking or rich-result benefit.
- A working visible search may declare `SearchAction`; Google's retired sitelinks
  search box must never be presented as an enabled feature.
- `FAQPage` remains appropriate for visible Q&A without a Google rich-result promise.
- Google's `speakable` beta targets eligible English-language US news/Assistant use.
- `rel="next"`/`rel="prev"` are valid HTML hints, not Google indexing signals.

## Crawlers and answer surfaces

- Keep `robots.txt`, sitemap, canonical URLs, and index policy consistent.
- Treat search, answer retrieval, model training, and user browsing as separate
  crawler purposes chosen by the owner for discovery, privacy, licensing, and policy.
- `llms.txt` is optional informal discovery metadata, not proof of crawler consumption.
- `humans.txt` is optional project provenance for people.
- Use concise answers, semantic lists/tables, and clear definitions when they improve
  the reader's page; never add hidden search-engine text.
- Do not claim crawler behavior, rankings, snippets, or AI citation outcomes that the
  generated site cannot verify.

## Performance

Use Astro image/static-output primitives and the repository's declared performance
gate. Avoid duplicate preload, unnecessary client hydration, and layout-shifting media.

Core Web Vitals field targets are LCP <=2.5 seconds, INP <=200 milliseconds, and CLS <=0.1
at the 75th percentile separately for mobile and desktop. Lighthouse cannot prove field compliance.
