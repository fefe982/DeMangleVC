#include <iostream>
class A {
public:
    int a;
    void b() {}
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

template<template<typename, typename> typename T> class temp_temp {};
temp_temp<std::basic_ostream> temp_temp_ostream;

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

temp_t_v<void (A::*)(), &A::b> temp_t_v_pmemfun;

temp_t_v<nullptr_t, nullptr> temp_t_v_nullptr;

// pack
template<typename...T> class temp_t_pack {};
temp_t_pack<> temp_t_pack_none;
temp_t_pack<int> temp_t_pack_int;
temp_t_pack<int, char> temp_t_pack_int_char;

template<typename...T, typename ...U> void func_packpack(std::tuple<T...>, std::tuple<U...>) {};

template<typename...T, typename K, typename ...U> void func_packtpack(std::tuple<T...>, K, std::tuple<U...>) {};

void func() {
    std::cout << pi<int> << pi<char*>;
    // a `$$Z` is used between packs
    func_packpack(std::tuple<>(), std::tuple<>());
    func_packpack(std::tuple<int>(1), std::tuple<>());
    func_packpack(std::tuple<>(), std::tuple<int>(1));
    func_packpack(std::tuple<int, short>(1, 2), std::tuple<long>(3));
    func_packpack(std::tuple<int>(1), std::tuple<short, long>(2, 3));

    // no `$$Z` appears after any pack, and may have resulted the last two
    // functions (one in comment) to be treated as the same function
    func_packtpack(std::tuple<>(), 1, std::tuple<>());
    func_packtpack(std::tuple<int>(1), 1, std::tuple<>());
    func_packtpack(std::tuple<>(), 1, std::tuple<int>(1));
    func_packtpack(std::tuple<int, short>(1, 2), 1, std::tuple<long>(3));
    func_packtpack(std::tuple<int>(1), 1, std::tuple<short, long>(2, 3));
    func_packtpack(std::tuple<int, short>(1, 2), 'a', std::tuple<long>(3));
    // currently the latter is treated as the same function as the previous one,
    // and would generate a C2664 for "cannot convert argument 1 from ......"
    // func_packtpack(std::tuple<int>(1), short(2), std::tuple<char, long>('a', 3));
}