# Visual studio mangling schema

This is mostly guessed from reverse engineering. A context-free grammar description of the schema is available in [CFG.txt](CFG.TXT).

## `Declaration`

The full symbol represetion the declaration of an entity. It always starts with `?`. User defined entities always starts with `?`. There are some compiler-generated entities that do not start with `?`, those out of scope of this article.

The basic structure of a declaration is `? QualifiedName IndetType`, that is a qualified followed by its type. There are some compiler-generated symbols uses something different from this pattern. They are also included in [CFG.txt](CFG.TXT) and discussed in ["special symbols"](special_symbols.md). They will not be included here.

## `QualifiedName`

C++ has namespaces and classes. The names are always "qualified". In the mangled symbol, it includes a `BaseName` and a possible empty list of `ClassNamespaceName`s. The list of `ClassNamespaceName`s, even if it is empty, ends with a `@`. The order of `ClassNamespaceName` is in reverse to what they do in a C++ qualified-id: inner first scope comes first.

For example:

```nohighlight
?width@ios_base@std@@QBE_JXZ
 ^^^^^^ BaseName
       ^^^^^^^^^ ClassNamespaceName
                ^^^^ ClassNamespaceName
                    ^ End of list of ClassNamespaceName

public: __int64 __thiscall std::ios_base::width(void)const
                           ^^^^^^^^^^^^^^^^^^^^ demangled qualified name
```

### `StringName`

As we can see from the example above, names are encoded in the mangled name directly, with an additional `@` to mark its end. The character used in these names are the same as those used in the C++ source.

VC++ supports non-ASCII characters in identifiers, as least in the 2019 commnity version. They character as saved directly into the mangled string, like `?你好@@3PBDB`. The string would use whatever byte sequence that is used in the source file. This means the mangled string would have the same encoding as the source file. For *universal-character-name* (like `\u03C0`), system encoding would be used. If the source file is not using the system encoding, two different encodings can mix up. (Note: C# handles things in UTF-8, no binary, so DemangleVC program cannot handle these input directly unless they are already in UTF-8. You must convert the strings into UTF-8 before feading it to DemangleVC, or you may get a bunch of question marks(`?`) in the output.)

Aside from normal names, this string name can be used for compiler generated names. The compiler generated names have characters usually not allowed in C++. For example, a lambda would be encoded as something like `<lambda_c6753c90be971fc9cc00b4f35f237a75>@`. A compiler generated static initialization guard use the name `$TSS0@`.

### `BaseName`

A `BaseName` can be an encoded string, or a *template-id*, or an "operator".

A template-id is in the form of `template_name<int, short>`. We would discuss it later, as it is a bit complex.

The "operator" here is not a usual C++ operator. Actually a lot things that are not C++ operators a encoded in the same way. They are referred to as `OperationName` here.

### `OperatorName`

`OperatorName` begins with `?`, followed by zero to two underscores, and a digit or capital letter. But note not all names in this form is an `OperatorName`. A list of `OperaterName`s can be found in [CFG.TXT](CFG.TXT).

All C++ overloadable operators are encoded in this way. Unary and binary `operator *`, use the same name, and so do the unary and binary `operator &`, prefix and postfix `operator++`, `operator--`. They are just different overloads.

Constructors and destructors are also encoded in this way. In C++, they do not have names.

User-defined literal operator is a bit different, as it has a name. It is encoded as `'?__K' StringName`, `?__K` followed by a `StringName`, which is its name, e.g.

```nohighlight
??__K_a@@YAPBDPBD@Z`
 ^^^^ user defined literal operator
     ^^^ name
       (^ end of empty ClassNamespaceName list)

char const * __cdecl operator "" _a(char const *)
                     ^^^^^^^^^^^^^^ demangled name
```

`operator ""_a` and `operator "" _a` are encoded in exact the same way. These two are a little different in C++ during syntax check, and may render one ill-formed but the other not. However, whenever they are both valid, they should be equivalent.

A lot of compiler-generated symbols are encoded in this way, some of which are not even functions. They also have some special formats for `IdentType` part. Check [special symbols](special_symbols.md) for details.

### `ClassNamespaceName`

`ClassNamespaceName` also includes `StringName`, which can be a class or namespace, and *template-id*, but no `OperatorName`. It has some other altertanatives: anonymous namespace, full declarator, and scope index.

Anonymous namespace would have a generated name, which is in the format of `'?A0x' "[0-9a-f]+" '@'`, such as:

```nohighlight
?pb@?A0xf3433384@@3PBDB
    ^^^^^^^^^^^^^ anonymous namespace.

char const * `anonymous namespace'::pb
             ^^^^^^^^^^^^^^^^^^^^^
```

Note: the demangled output is not valid C++, but followers UndecoratedSymbolNames notation.

Full declarator and scope index are used for local scope static or thread_local variables. The are in face "global" (not on the stack), so they need a name. The full declarator part is the `Declaration` of the function they are declared in, and the scope index is an integer showing which scope (within which pair of `{}`) it is declared in. For example:

```nohighlight
?b@?1??func@@YAXXZ@4HA
    ^ Integer constant 2
      ^^^^^^^^^^^^ Declaration of the function

int `void __cdecl func(void)'::`2'::b
```

Note: the demangled output is not valid C++, but followers UndecoratedSymbolNames notation.

The scope index is in the format of `'?' IntConst`, which `IntConst` is an integer. The format of `IntConst` is discussed in a separate section.

The full declaration part is in the format of `'?' Declaration`.

### BackRefStringName

In the whole `Declaration`, the first ten string pieces are numbered 0~9, and if they appear again, they are referenced with just a digit. This index is shared withen the whole `Declaration`, except for a *template-id*, which has its own index. The `Declaration` in full declaration part of a `ClassNamespaceName` also shares the index with outer `Declaration` (or *template-id*).

`StringName`, anonymous namespace, *template-id*, are all counted. But scope index, `OperatorName` are not.

For example:

```nohighlight
?aaa@?1??0aab@aac@0@YAXXZ@4HA
 ^^0      ^^1 ^^2
         ^ 0 -- <a>
                  ^ 0 -- <b>

int `void __cdecl aaa::aac::aab::aaa(void)'::`2'::aaa
                  <b>            <a>              ^^^base
```

`aaa` appear three times in this `Declaration`, two are expressed using back reference. Two of `aaa`s are in a sub `Declaration`. The three `aaa` are all differen kind of entity: variable, function, and namespace. In the sub `Declaration`, the `Basename` part is a `BackRefStringName`.
