# Visual Studio mangling scheme

This is mostly guessed from reverse engineering. A context-free grammar description of the schema is available in [CFG.txt](CFG.TXT).

## `IntConst`

When an integer is to be encoded, the following scheme is used:

* `0` to `9` are used to represent integers 1 to 10, `0` for 1, `9` for 10.
* The numbers 0 and other positive integers are represented in hexdecimal, but the digits used are `A` (for 0) to `P` (for 15). The hexdecimal number ends with a `@`.
* Minus number has prefix of `?`, followed the encoded form of the absolute value.

For example:

* `0` : 1
* `9` : 10
* `A@` : 0
* `?1` : -2
* `?K@` : -11

## `QualifiedName`

C++ has namespaces and classes. The names are always “qualified”. In the mangled symbol, it includes a `BaseName` and a `NameQualifier`, which a possible empty list of `ClassNamespaceName`s. The list of `ClassNamespaceName`s, even if it is empty, ends with a `@`. The order of `ClassNamespaceName` is in reverse to what they do in a C++ qualified-id: inner first scope comes first.

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

VC++ supports non-ASCII characters in identifiers, as least in the 2019 community version. The characters as saved directly into the mangled string, like `?你好@@3PBDB`. The string would use whatever byte sequence that is used in the source file. This means the mangled string would have the same encoding as the source file. For *universal-character-name* (like `\u03C0`), system encoding would be used. If the source file is not using the system encoding, two different encoding can mix up. (Note: C# handles things in UTF-8, no binary, so DemangleVC program cannot handle these input directly unless they are already in UTF-8. You must convert the strings into UTF-8 before feeding it to DemangleVC, or you may get a bunch of question marks(`?`) in the output.)

Aside from normal names, this string name can be used for compiler generated names. The compiler generated names have characters usually not allowed in C++. For example, a lambda would be encoded as something like `<lambda_c6753c90be971fc9cc00b4f35f237a75>@`. A compiler generated static initialization guard use the name `$TSS0@`.

### `BaseName`

A `BaseName` can be an [encoded string](#stringname), or a [*template-id*](#templatename), or an [“operator”](#operatorname).

Plain strings are encoded using [`StringName`](#stringname).

C++ *template-id* is *template-name* with the *template-argument-list*, e.g. `template_name<int, short>`. It is encoded as a [`TemplateName`](#templatename).

The “operator” here is not only a usual C++ operator. Actually a lot of things that are not C++ operators are encoded in the same way. They are all referred to as `OperationName` here.

### `OperatorName`

An `OperatorName` begins with `?`, followed by zero to two underscores, and a digit or capital letter. But note not all names in this form is an `OperatorName`. A list of `OperaterName`s can be found in [CFG.TXT](CFG.TXT).

All C++ overloadable operators are encoded in this way. Unary and binary `operator *` use the same name, and so do the unary and binary `operator &`, prefix and postfix `operator++`, `operator--`. They are just different overloads.

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

`ClassNamespaceName` also includes `StringName`, which can be a class or namespace, and *template-id*, but no `OperatorName`. It has some other alternatives: anonymous namespace, full declaration, and scope index.

#### Anonymous namespace

Anonymous namespace would have a generated name, which is in the format of `'?A0x' "[0-9a-f]+" '@'`, such as:

```nohighlight
?pb@?A0xf3433384@@3PBDB
    ^^^^^^^^^^^^^ anonymous namespace.

char const * `anonymous namespace'::pb
             ^^^^^^^^^^^^^^^^^^^^^
```

Note: the demangled output is not valid C++, but follows `UnDecorateSymbolName` notation.

#### Full declaration

Full declaration and scope index are used for local scope `static` or `thread_local` variables. They are in face “global” (not on the stack), so they need a name. The `NameQualifier` part shows the scope which the variable is declared in. The full declaration part is the `Declaration` of the function they are declared in, and the scope index is an integer showing which scope (within which pair of `{}`) it is declared in. For example:

The full declaration part is in the format of `'?' Declaration`.

The scope index is in the format of `'?' IntConst`, where [`IntConst`](#intconst) is a positive integer.

```nohighlight
?b@?1??func@@YAXXZ@4HA
    ^ Integer constant 2
      ^^^^^^^^^^^^ Declaration of the function

int `void __cdecl func(void)'::`2'::b
```

Note: the demangled output is not valid C++, but follows `UnDecorateSymbolName` notation.

The `main` function and `extern "C"` function are not mangled in a C++ way, the full declaration used here is not the actual function declaration, but would treat the full qualified name of the function as a special variable. See [`VarKind`](#varkind) for more details.

### `BackRefStringName`

In the whole `Declaration`, the first ten string pieces are numbered 0~9, and if they appear again, they are referenced with just a digit. This index is shared within the whole `Declaration`, except for [`TemplateName`](#templatename), which has its own index. The `Declaration` in full declaration part of a `ClassNamespaceName` also shares the index with outer `Declaration` (or `TemplateName`).

`StringName`, anonymous namespace, `TemplateName`, are all counted. But scope index, `OperatorName` are not.

For example:

```nohighlight
?aaa@?1??0aab@aac@0@YAXXZ@4HA
 ^^0      ^^1 ^^2                numbering of strings
         ^ 0                     <a> referring to string number 0
                  ^ 0            <b> referring to string number 0, again

int `void __cdecl aaa::aac::aab::aaa(void)'::`2'::aaa
                  <b>            <a>              ^^^base
```

`aaa` appears three times in this `Declaration`, two are expressed using back reference. Two of `aaa`s are in a sub `Declaration`. The three `aaa` are all different kinds of entity: variable, function, and namespace. In the sub `Declaration`, the `Basename` part is a `BackRefStringName`.

## `Type`

### `BaseType`

All “basic” types in C++. Most of them use a simple capital letter or an underscore followed by a single capital letter. `nullptr_t` uses `$$T`. Check [CFG.TXT](CFG.TXT) for a full list.

### `CompoundType`

Types for `class`, `union`, `struct`, `enum`, `enum class`. It uses the form of `CompoundSpLt QualifiedName`.

`CompoundSpLt` is `T` (`union`), `U` (`struct`), `V` (`class`) or `W[1-7]` (`enum` / `enum class`). The number following `W` shows the base type of the `enum`, but currently it seems that `W4` is always used.

The `QualifiedName` here is the name of the type.

### `ArrType`

`ArrType` is type for array. Variables of array type are all mangled as pointers. However, arrays type are still needed as they can appear in template argument, or in pointer or reference to array.

For a `k` dimension array, it starts with `Y`, followed by `k+1` `IntConst`, and then the `Type` of array element.

The first `IntConst` is the number of dimension of the array. The following `IntConst` specifies the length of each dimension of the array. Array of unknown bound would have `0` for the length of that dimension.

```nohighlight
?m_array@@3PAY30123DA
          ^           namespace scope
           ^          pointer
            ^         no cv-qualifier (array cannot have cv-qualifier)
             ^        array
              ^       4 dimensions
               ^^^^   lengths of the four dimensions
                   ^  char (array element type)

char (* m_array)[1][2][3][4]
```

### `CVQVar`

`CVQVar` shows whether the variable is `const` and/or `volatile`.

`CVQVar` is often a `SimpleCVQVar`, which is `A`, `B`, `C` and `D`, meaning no *cv-qualifier*, `const`, `volatile`, `const volatile`, respectively.

`SimpleCVQVar` can be prefixed by `E`, meaning `__ptr64`.

For pointer to member, `CVQVar` also includes a `NameQualifier` for the class, in the form of `MemberCVQVar NameQualifer`. `MemberCVQVar` uses `Q`, `R`, `S` and `T` to be distinguished from `SimpleCVQVar`.

For example:

```nohighlight
?pmem_p@@3PQA@@HQ1@
 ^^^^^^^            Basename
        ^           empty NameQualifier
         ^          namespace scope
          ^         pointer
           ^        no cv-qualifer for pointer to member
            ^^^     A:: for pointer to member
               ^    int, type pointed
                ^   Final CVQVar, no cv-qualifer for pointer to member
                 ^^ A::, using BackRefStringName

int A::* pmem_p
```

Here, the *cv-qualifier* information is also encoded in the pointer type, so the final `CVQVar` (`Q1@`) is ignored. See [`RefType`](#reftype), [`VariableType`](#variabletype) for more information.

### `RefType`

Pointers and references are mangled as `RefType`. However, some types that are not pointers or references also uses `RefType`. Unlike `BaseType` and `CompoundType`, `RefType` has `cv-qualifier` encoded within the type.

It takes the basic form of `RefKind CVQVar Type`, while `RefKind` shows the kind of the type (pointer or reference, with *cv-qualifier* encoded), `CVQVar Type` is the referenced type. For pointer to member, the class information is also encoded in `CVQVar` (name only). When `Type` also has *cv-qualifier*, (e.g., when `Type` is a `RefType`, ) the *cv-qualifier* in `CVQVar` and `Type` should be the same.

The `RefKind` for pointers and references are:

* `A` : l-value reference `&`
* `B` : `& volatile`, currently disallowed by C++.
* `P` : pointer `*`
* `Q` : `* const`
* `R` : `* volatile`
* `S` : `* const volatile`
* `$$Q` : r-value reference `&&`

Other `RefKind` are:

* `?` : Used for cv-qualified `CompoundType`. `CompoundType` does not have a cv-qualifier encoded. So when a cv-qualified `CompoundType` is needed (as template parameter, function return type, etc.), a `RefType` with `?` is constructed to add a cv-qualifier.
* `$$A` : Used for function, e.g. in template parameter.
* `$$B` : Used for array, e.g. in template parameter. Arrays do not have *cv-qualifier*, so the `CVQType` part is omitted. It takes the form of `$$B ArrType`.
* `$$C` : Used for array element, when it is cv-qualified, but the type itself do not encode a cv-qualifier (cv-qualified `BaseType`, for example)

#### Function pointer / reference

When function appear in a `RefType` to form a function pointer, it takes the form of `NonMemCVQFunc FuncP` or `MemCVQFunc NameQualifier CVQThis FuncP`, for non-member or static functions pointers, and pointer to member function, respectively. Check [Function](#function) to find an introduction for each part.

### `BackRefType`

Just like string, types that has already appeared in the mangled name can also be referred to using `0` to `9`. Only “full” type are counted. For example, for a “pointer to pointer to int” type, the “pointer to int” type within cannot be referred to as it is not a full type. Types that are represented using only one character (most `BaseType`s) is not counted.

`TemplateName` has its own back reference count for types.

## Function

The components constructing a function may be a little different according where the function appears. Here each component is described.

### `NonMemCVQFunc`

When a non-member function or static member function appear in a `RefType`, in place of `CVQVar`, `NonMemCVQFunc` (`6`) is used.

### `MemCVQFunc`

In a pointer to member, in the place of `CVQVar`, `MemCVQFunc` (`8`) is used. It is then followed by a `NameQualifier`, specifying the class; and then a `CVQThis`, specifying the *cv-qualifier-seq* and *ref-qualifier* after the parameter list for member functions.

### `CVQThis`

`CVQThis` is basically the same as `CVQVar`, and also be `CVQVar` prefixed by `G` for *ref-qualifier* `&`, or `H` for *ref-qualifier* `&&`.

### `MemFuncMod`

A single capital letter. Used for member function, showing whether the function is `public`, `protected`, `private`, `virtual`, `static`. See [CFG.TXT](CFG.TXT) for a list.

### `NonMemFuncMod`

Used as a counterpart for `NonMemFuncMod`. Use the letter `Y`.

### `FuncP`

`FuncP` includes the calling convention, return type, parameter list, and exception specification, in the form of `CallConv RetType ParamList ExceptSpecifier`

### `CallConv`

`CallConv` is a single capital letter showing the calling convention of the function. Refer to [CFG.TXT](CFG.TXT) for a list of them.

### `RetType`

`RetType` is usually a [`Type`](#type), but it can also be `@` for functions that does not have a return type, e.g. constructors and destructors. Functions returning `void` is represented by `X`, as is shown in `BaseType`.

### `ParamList`

`ParamList` is usually a list of `Type`s, ended with `@`.

For function with no parameters, `X` (without the ending `@`) is used. `X` represents `void` in `BaseType`.

The ending `@` can be replaced by `Z`, which show that the parameter list of the function ends in ellipsis (`...`). If the whole parameter list is an ellipsis, it is represented as a single `Z`.

### `ExceptSpecifier`

It uses the same format as `ParamList`.

C++ has exception specification for functions, which used to use *dynamic-exception-specification* (`throw(int, short, std::exeception)`). The `ExceptSpecifier` is a list of types mentioned in this *dynamic-exception-specification*. A single `Z` is used when there is no exception specification. This is how `UnDecorateSymbolName` interprets the `ExceptSpecifier`.

However, as this is not actually implemented, and the *dynamic-exception-specification* being replaced by *noexcept-specifier*, VC++ actually only generates `Z` for the exception specification.

## `Declaration`

The full symbol represents the declaration of an entity. It always starts with `?`. Functions and variables declared in the source file always starts with `?`, when it is mangled as C++. There are some compiler-generated entities that do not start with `?`, those are out of scope of this article.

The basic structure of a declaration is `? QualifiedName IdentType`, which is a qualified name followed by its type. There are some compiler-generated symbols uses something different form from this pattern. They are also included in [CFG.txt](CFG.TXT) and discussed in [“special symbols”](special_symbols.md). They will not be included here.

## `IdentType`

A `Declaration` can be a variable or a function. `IdentType` is a `VariableType` for variable, `NonMemFuncMod FuncP` for non-member function, and `MemFuncMod CVQThis FuncP` for member functions.

### `VariableType`

A `VariableType` consists of three part, `VarKind Type CVQVar`. `VarKind` shows the “kind” of the variable. `Type` and `CVQVar` combined should be the type of the variable in the C++ point of view.

In the C++ point of view, `CVQVar` is part of the type information, but not the variable. However, VC++ mangles the top-level CV-qualifier differently, separated from the `Type`. When *cv-qualifier* is also encoded in `Type` (such as `RefType`), the final `CVQVar` is ignored.

#### `VarKind`

`VarKind` is a single digit from `0` to `9`. Normal variables are classified into five groups, with numbers `0` to `4`. And there are some special variables, which use numbers `5` to `9`:

* `0`: private static member
* `1`: protected static member
* `2`: public static member
* `3`: namespace scope variable
* `4`: local scope static variable
* `5`: local static guard
* `6`: vftable / RTTI Complete Object Locator
* `7`: vbtable
* `8`: RTTI Type Descriptor / RTTI Base Class Descriptor / RTTI Base Class Array / RTTI Class Hierarchy Descriptor
* `9`: the `main` function / `extern "C"` function

`5` ~ `8` are used for compiler generated special variables, and they have a special structure in the mangled name. See [CFG.txt](CFG.TXT) and [special symbols](special_symbols.md) for more details.

`9` was used in the full declaration of the function in local scope static variable, if the function is the function `main`, or a function that is declared `extern "C"`. The name of the function is treated as special variable, with number `9`, and nothing follows. No `Type CVQVar` part exists.

```nohighlight
?local_c_in_c_function@?1??c_function@@9@4VC@@A
 ^^^^^^^^^^^^^^^^^^^^^                            BaseName
                       ^^                         Scope Index
                          ^^^^^^^^^^^^^^          Full declaration for the function
                                       ^          Special variable kind '9'
                                        ^         End of ClassNamespaceName List

class C `c_function'::`2'::local_c_in_c_function
```

## `TemplateName`

A `TemplateName` (represents *template-id* in C++) can appear in `BaseName` or `ClassNamespaceName`. It has the form `'?$' BaseName TplArgList`.

`TplArgList` is a (possible empty) list of `TplArg`, terminated with `@`. `TplArg` represent a template argument, or empty template parameter expansion pack, or separator between consecutive template parameter expansion pack.

There is separate string and type back reference count inside each `TemplateName`. The whole `TemplateName` would be counted as one string in the outer `Declaration` or `TemplateName`.

### `TplArg`

For type template argument, `Type` is used. Top-level array or function can only appear int `TplArg`.

For template template argument, `Type` is also used. The *template-name* is treated as having the type of the instantiated template. It cannot be distinguished with a type template argument in the mangled name.

For non-type template argument, arguments of integral type, `nullptr_t` or pointer to data member are represented with a `IntConst`, prefixed by `?0`, forming an `IntTplArg`. The type information is lost in the mangled string.

Other pointer or reference type non-type template parameter, `PointerTplArg` is used, which consist of `'?1' Declaration`. It is the full declaration of the entity referred to prefixed by `?1`. Pointer and reference are represented in the same way.

If there is a template parameter pack, and no template argument for it, `$$V` is inserted in the place of template parameter pack for type / template template parameter, and `$S` for non-type template parameter.

If there are two consecutive template parameter packs in the template, a `$$Z` is always inserted between them.
