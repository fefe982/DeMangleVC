template <class T>
struct remove_reference {
    using type = T;
};

template <class T>
struct remove_reference<T&> {
    using type = T;
};

template <class T>
struct remove_reference<T&&> {
    using type = T;
};

template <class T>
using remove_reference_t = typename remove_reference<T>::type;

template <class T>
remove_reference_t<T>&& move(T&& _Arg) noexcept {
    return static_cast<remove_reference_t<T>&&>(_Arg);
}
class A {
public:
    // special member, no return value
    A() {}
    ~A() {}

public:
    static int data_pub;
    static void func_static_pub() {
        func_static_protect();
        func_static_private();
    }
    virtual void func_virtual_pub() {}
    void func_pub() {
        func_protect();
        func_private();
    }
protected:
    static int data_protect;
    static void func_static_protect() {}
    virtual void func_virtual_protect() {}
    void func_protect() {}
private:
    static int data_private;
    static void func_static_private() {}
    virtual void func_virtual_private() {}
    void func_private() {}

public:
    void func_c()const {}
    void func_v()volatile {}
    void func_cv()const volatile {}
    void func_ref()& {}
    void func_ref_c()const& {}
    void func_ref_v()volatile& {}
    void func_ref_cv()const volatile& {}
    void func_rref()&& {}
    void func_rref_c()const&& {}
    void func_rref_v()volatile&& {}
    void func_rref_cv()const volatile&& {}
};

// pointer to member
int A::* pmem_p = nullptr;
const int A::* pmem_p_c = nullptr;
volatile int A::* pmem_p_v = nullptr;
const volatile int A::* pmem_p_cv = nullptr;
int A::* const pmem_c_p = nullptr;
int A::* volatile pmem_v_p = nullptr;
int A::* const volatile pmem_cv_p = nullptr;

void (A::* pfunc_p)() = nullptr;
void (A::* const pfunc_c_p)() = nullptr;
void (A::* volatile pfunc_v_p)() = nullptr;
void (A::* const volatile pfunc_cv_p)() = nullptr;

auto dummy01 = &pmem_c_p;
auto dummy02 = &pmem_cv_p;
auto dummy03 = &pfunc_c_p;
auto dummy04 = &pfunc_cv_p;

int A::data_pub;
int A::data_protect;
int A::data_private;

void func() {
    A a;
    A::func_static_pub();
    a.func_virtual_pub();
    a.func_pub();
    a.func_c();
    a.func_v();
    a.func_cv();
    a.func_ref();
    a.func_ref_c();
    a.func_ref_v();
    a.func_ref_cv();
    move(a).func_rref();
    move(a).func_rref_c();
    move(a).func_rref_v();
    move(a).func_rref_cv();
}