# CodeColoring

Regex-based syntax highlighting for `CodeDocument`.

The tokenizer (`PrismTokenizer`) and the language grammars under `Languages/`
are C# ports of [Prism.js](https://github.com/PrismJS/prism), used under the
MIT license (Copyright (c) 2012 Lea Verou). See `THIRD-PARTY-NOTICES.md` at
the repository root for the full license text.

## Adding a language

Implement `ILanguage` in `Languages/`:

- `Id` is the canonical lowercase language name; `Aliases` are alternate names.
- `GetGrammar()` returns an ordered `Grammar` (token name → patterns). Order
  matters: earlier entries win.
- Languages are discovered automatically via assembly scanning in
  `LanguageRegistry`; no manual registration is needed.

When porting a grammar from Prism, note the JS → .NET regex differences:

- JS flags: `i` → `IgnoreCase`, `m` → `Multiline`, `s` → `Singleline` (pass
  via the `regexOptions` string on `Pattern`).
- The JS-only `[^]` character class must be rewritten as `[\s\S]`.
- Prism's `lookbehind: true` convention (capture group 1 is trimmed from the
  match) is supported directly by `Pattern.Lookbehind`.
- Prism's template helpers (`replace(/<<0>>/g, ...)`, nested patterns) map to
  `RegexUtils.Replace` and `RegexUtils.Nested`.
- `Prism.languages.extend` → `Grammar.Extend`; `insertBefore` →
  `Grammar.InsertBefore`.
