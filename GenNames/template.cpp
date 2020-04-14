using nullptr_t = decltype(nullptr);

class A {
public:
    int a;
    int b;
    int c;
    int d;
    int e;
    void func_a() {}
    void func_b() {}
    void func_c() {}
    void func_d() {}
};
template<typename> class temp_t {};
temp_t<int> temp_t_int;
temp_t<void> temp_t_void;
temp_t<int*> temp_t_p_int;
temp_t<const int*> temp_t_p_cint;
temp_t<int* const> temp_t_cp_int;
temp_t<int&> temp_t_ref;
temp_t<const int&> temp_t_cref;
temp_t<int&&> temp_t_rref;
temp_t<const int&&>temp_t_crref;
temp_t<int[3]> temp_t_arr3;
temp_t<int[]> temp_t_arr;
temp_t<int(*)[3]> temp_t_parr3;
temp_t<int(*)[]> temp_t_parr;
temp_t<int(&)[3]> temp_t_ref_arr3;
temp_t<int(&&)[3]> temp_t_rref_arr3;
temp_t<int(&)[]> temp_t_ref_arr;
temp_t<int(&&)[]> temp_t_rref_arr;
temp_t<void()> temp_t_func;
temp_t<void(*)()> temp_t_pfunc;
temp_t<void(&)()> temp_t_refunc;
temp_t<void(&&)()> temp_t_rrefunc;
temp_t<A> temp_t_class;
temp_t<A*> temp_t_class_p;
temp_t<A&> temp_t_class_ref;
temp_t<std::nullptr_t> temp_t_nullptr_t;

template<typename T> T pi = T();

template<typename, typename> class temp_type_tt;
template<template<typename, typename> typename T> class temp_temp {};
temp_temp<temp_type_tt> temp_temp_tt;

enum ENUM {
    e_a,
    e_b
};

// type of non-type parameter in the mangled name is usually not 
// recoverable.

// Integral types / pointer to data member / nullptr_t are encoded in the same way

// pointer and l-value reference are encoded in the same way
// As there is no C++ conformant way to show all the information encoded 
// for pointer and l-value reference non-type arguments,
// unlike UndecoratedSymbolName, comments are used to show these information 
template<typename T, T i> class temp_t_v {};
temp_t_v<int, -1> temp_t_v_int;
temp_t_v<char, 'c'> temp_t_v_char;
temp_t_v<unsigned long, ~1UL> temp_t_v_ulong;
temp_t_v<ENUM, e_b> temp_t_v_enum;

int i;
temp_t_v<int*, &i> temp_t_v_pint;
temp_t_v<int* const, &i> temp_t_v_cpint;

void func();
temp_t_v<void(*)(), &func> temp_t_v_pfunc;
temp_t_v<void(* const)(), &func> temp_t_v_cpfunc;

temp_t_v<int&, i> temp_t_v_ref;
temp_t_v<void(&)(), func> temp_t_v_ref_func;

temp_t_v<int A::*, &A::a> temp_t_v_pmember;
temp_t_v<int A::* const, &A::a> temp_t_v_cpmember;

temp_t_v<void (A::*)(), &A::func_b> temp_t_v_pmemfun;

temp_t_v<nullptr_t, nullptr> temp_t_v_nullptr;

// pack
template<typename...T> class temp_t_pack {};
temp_t_pack<> temp_t_pack_none;
temp_t_pack<int> temp_t_pack_int;
temp_t_pack<int, char> temp_t_pack_int_char;

template<typename...> class tuple{};

template<typename...T, typename ...U> void func_packpack(tuple<T...>, tuple<U...>) {};

template<typename...T, typename K, typename ...U> void func_packtpack(tuple<T...>, K, tuple<U...>) {};

#define PACKTEST_DECL(type, suf) \
template<type...i> class tuple_##suf {};\
template<type...T, type ...U> void func_##suf##_packpack(tuple_##suf<T...>, tuple_##suf<U...>) {}; \
template<type...T, type K, type ...U> void func_##suf##_packipack(tuple_##suf<T...>, tuple_##suf<K>, tuple_##suf<U...>) {};

#define PACKTEST_CALL(type, suf, i1, i2, i3, i4) \
    func_##suf##_packpack(tuple_##suf<>(), tuple_##suf<>()); \
    func_##suf##_packpack(tuple_##suf<i1>(), tuple_##suf<>()); \
    func_##suf##_packpack(tuple_##suf<>(), tuple_##suf<i1>()); \
    func_##suf##_packpack(tuple_##suf<i1, i2>(), tuple_##suf<i3>()); \
    func_##suf##_packpack(tuple_##suf<i1>(), tuple_##suf<i2, i3>()); \
    func_##suf##_packipack(tuple_##suf<>(), tuple_##suf<i1>(), tuple_##suf<>()); \
    func_##suf##_packipack(tuple_##suf<i1>(), tuple_##suf<i2>(), tuple_##suf<>()); \
    func_##suf##_packipack(tuple_##suf<>(), tuple_##suf<i1>(), tuple_##suf<i2>()); \
    func_##suf##_packipack(tuple_##suf<i1, i2>(), tuple_##suf<i3>(), tuple_##suf<i4>());

PACKTEST_DECL(typename, t)
PACKTEST_DECL(int, i)
PACKTEST_DECL(int A::*, pmem)

template<void (A::*...i)()> class tuple_pmemfunc {};
template<void (A::*...T)(), void (A::*...U)()> void func_pmemfunc_packpack(tuple_pmemfunc<T...>, tuple_pmemfunc<U...>) {};
template<void (A::*...T)(), void (A::* K)(), void (A::*...U)()> void func_pmemfunc_packipack(tuple_pmemfunc<T...>, tuple_pmemfunc<K>, tuple_pmemfunc<U...>) {};

PACKTEST_DECL(template <typename> typename, temp)

template<typename> class temp_temp_00 {};
template<typename> class temp_temp_01 {};
template<typename> class temp_temp_02 {};
template<typename> class temp_temp_03 {};

void func() {
    auto _1 = pi<int>;
    auto _2 = pi<char*>;
    // a `$$Z` is used between packs
    func_packpack(tuple<>(), tuple<>());
    func_packpack(tuple<int>(), tuple<>());
    func_packpack(tuple<>(), tuple<int>());
    func_packpack(tuple<int, short>(), tuple<long>());
    func_packpack(tuple<int>(), tuple<short, long>());

    // no `$$Z` appears after any pack, and may have resulted the last two
    // functions (one in comment) to be treated as the same function
    func_packtpack(tuple<>(), 1, tuple<>());
    func_packtpack(tuple<int>(), 1, tuple<>());
    func_packtpack(tuple<>(), 1, tuple<int>());
    func_packtpack(tuple<int, short>(), 1, tuple<long>());
    func_packtpack(tuple<int>(), 1, tuple<short, long>());
    func_packtpack(tuple<int, short>(), 'a', tuple<long>());
    // currently the latter is treated as the same function as the previous one,
    // and would generate a C2664 for "cannot convert argument 1 from ......"
    // func_packtpack(tuple<int>(), short(2), tuple<char, long>());

    PACKTEST_CALL(int, i, 1, 2, 3, 4);
    PACKTEST_CALL(int A::*, pmem, &A::a, &A::b, &A::c, &A::d);
    PACKTEST_CALL(void (A::*)(), pmemfunc, &A::func_a, &A::func_b, &A::func_c, &A::func_d);
    PACKTEST_CALL(template <typename> typename, temp, temp_temp_00, temp_temp_01, temp_temp_02, temp_temp_03);
}