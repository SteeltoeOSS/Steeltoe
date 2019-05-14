# Coding Guide

This guide provides instructions which you can follow that will help you during your coding of contributions to Steeltoe.

## C# Coding Style

 While we likely are not completely consistent throughout our code base, we are striving to be better, so please try to follow our coding guidelines. The general set of rules we try to follow is to use "Visual Studio defaults".  As an FYI, most of these guidelines come from the `corefx` C# coding style guide.

1. We use [Allman style](https://en.wikipedia.org/wiki/Indent_style#Allman_style) braces, where each brace begins on a new line. A single line statement block can go without braces but the block must be properly indented on its own line and it must not be nested in other statement blocks that use braces.
1. We use four spaces of indentation (no tabs).
1. We use `_camelCase` for internal and private fields and use `readonly` where possible. Prefix instance fields with `_`, static fields with `s_` and thread static fields with `t_`. When used on static fields, `readonly` should come after `static` (i.e. `static readonly` not `readonly static`).
1. We avoid `this.` unless absolutely necessary.
1. We always specify the visibility, even if it's the default (i.e. `private string _foo` not `string _foo`). Visibility should be the first modifier (i.e. `public abstract` not `abstract public`).
1. Namespace imports should be specified at the top of the file, *outside* of `namespace` declarations and should be sorted alphabetically.
1. Avoid more than one empty line at any time. For example, do not have two blank lines between members of a type.
1. Avoid spurious free spaces. For example avoid `if (someVar == 0)...`, where the dots mark the spurious free spaces. Consider enabling "View White Space (Ctrl+E, S)" if using Visual Studio, to aid detection.
1. If a file happens to differ in style from these guidelines (e.g. private members are named `m_member` rather than `_member`), the existing style in that file takes precedence.
1. We only use `var` when it's obvious what the variable type is (i.e. `var stream = new FileStream(...)` not `var stream = OpenStandardInput()`).
1. We use language keywords instead of BCL types (i.e. `int, string, float` instead of `Int32, String, Single`, etc) for both type references as well as method calls (i.e. `int.Parse` instead of `Int32.Parse`).
1. We use PascalCasing to name all our constant local variables and fields.
1. We use ```nameof(...)``` instead of ```"..."``` whenever possible and relevant.
1. Fields should be specified at the top within type declarations.
1. When including non-ASCII characters in the source code use Unicode escape sequences (\uXXXX) instead of literal characters. Literal non-ASCII characters occasionally get garbled by a tool or editor.

## Cross-platform Considerations

All of the Steeltoe frameworks should work on the CoreCLR, which supports multiple operating systems, so don't assume everyone is developing and running on Windows.
Code should be sensitive to the differences between OS's and so here are some specifics to consider.

* Line breaks - Windows uses \r\n, OS X and Linux uses \n. When it is important, use Environment.NewLine instead of hard-coding the line break. Note: this may not always be possible or necessary. Be aware that these line-endings may cause problems in code when using @"" text blocks with line breaks.
* Environment Variables - OS's use different variable names to represent similar settings. Code should consider these differences. For example, when looking for the user's home directory, on Windows the variable is `USERPROFILE` but on most Linux systems it is HOME.
* File path separators -  Windows uses \ and OS X and Linux use / to separate directories. Instead of hard-coding either type of slash, use Path.Combine() or Path.DirectorySeparatorChar. If this is not possible (such as in scripting), use a forward slash. Windows is more forgiving than Linux in this regard.

## Unit Tests

The unit tests for a package should be put in a project with the same name and append `.Test` to the name. For example, for the package `Steeltoe.Extensions.Configuration.CloudFoundry`, the tests should be in a package with the name `Steeltoe.Extensions.Configuration.CloudFoundry.Test`. In general there should be exactly one unit test package for each runtime package.

The unit test class names should end with `Test` and live in the same namespace as the class being tested. For example, the unit tests for the `Steeltoe.Extensions.Configuration.CloudFoundry` class would be in `Steeltoe.Extensions.Configuration.CloudFoundryTest` class in the test package.

Use the plethora of built-in assertions from `xUnit` and ensure you have no `xUnit` warnings during the build.

## Breaking Changes

In general, breaking changes can be made only in a new major product version, e.g. moving from 1.x.x to 2.0.0. Even still, we generally try to avoid breaking changes because they can incur large costs for anyone using our packages.
