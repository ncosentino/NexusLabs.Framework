---
applyTo: '**/*.{test,spec}.{jsx,tsx},**/*.svelte.{test,spec}.{js,ts}'
scope: 'frontend UI tests'
---

# Frontend UI tests

- Query in user-observable priority: role plus accessible name when stable and unique,
  label for form controls, visible text when it is the user-facing contract, then
  `data-testid` only when semantic queries cannot identify the target or when a stable
  container must scope a repeated entity.
- A test ID is not proof of interactive semantics. For repeated or localized controls,
  put the locale-independent ID on the entity container, call `within(...)`, and query
  the control by role inside that scope.
- When localization is the behavior under test, assert the translated accessible name
  separately from the stable entity lookup.
- Use `userEvent` or the framework-equivalent user interaction API for clicks, typing,
  selection, and focus. Avoid direct event dispatch when the test represents user
  behavior.
- Interactive-control tests assert the semantic role so replacing a button, link, or
  form control with a non-semantic element fails the test.

```ts
const row = within(screen.getByTestId(`account-row-${account.id}`));
const edit = row.getByRole('button');
expect(edit).toHaveAccessibleName(translatedEditLabel);
await user.click(edit);
```
