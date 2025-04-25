# Expathy

An F# library that is heavily inspired by [Thoth.Json](https://github.com/thoth-org/Thoth.Json) and aspires to be "Thoth.Json for XML". It has no external dependencies and leans on `System.Xml`'s XPath capabilities.

## To-do

- [x] Add path-tracing error messages
  - [ ] Allow multiple decoding errors
- [ ] Add custom validation/mapping like Thoth's `andThen`
- [ ] Add `Encoding` module
- [ ] Expose  `XmlNamespaceManager` config
- [ ] Add DSL/builder for XPath expressions
- [ ] Refactor into multiple source files

## Links

- [XPath Syntax - Microsoft Learn](https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-3.5/ms256471(v%3dvs.90))
- [Online XPath Playground](https://xpather.com/)

## Examples

See the [Samples](./Samples) directory for sample XML files and matching Expathy parsers.
