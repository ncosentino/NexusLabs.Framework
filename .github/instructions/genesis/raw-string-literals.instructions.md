---
applyTo: "**/*Generator*.cs,**/*Template*.cs,**/*Prompt*.cs,**/*Script*.cs,**/*Fixture*.cs,**/*Snapshot*.cs"
---

# Raw String Literal Rules

- Put a multi-line raw string's opening and closing `"""` on separate lines at
  the same indentation.
- Indent content to at least the closing delimiter's column; C# removes that
  common indentation.
- Do not leave the opening delimiter at the end of an assignment line.
- Single-line raw strings are exempt.
