---
applyTo: "go.mod,go.sum"
---

# Go modules and baseline verification

## Modules

Keep `go.mod` minimal and let `go mod tidy` manage the dependency graph. After
adding or removing an import, run `go mod tidy` and commit the resulting `go.mod`
and `go.sum` together. `go build` neither adds missing requirements nor removes
unused ones — `go mod tidy` does both.

## Everyday commands

```sh
go build ./...
go vet ./...
go test -race ./...
```

Run `go test` with `-race` in CI to catch data races.

Do not introduce a linter, formatter, generator, or release command unless the
repository declares that tool through configuration, dependencies, workflows, or
project-owned guidance.

## CI

Pin the Go version with `go-version-file: go.mod` rather than hardcoding it in
the workflow. Verify module hygiene by running `go mod tidy` and failing on any
diff:

```sh
go mod tidy
git diff --exit-code go.mod go.sum
```
