#include <experimental/coroutine>
class B {
public:
    bool await_resume() { return true; }
    int get_return_object() { return 1; }
    bool initial_suspend() { return false; }
    bool final_suspend() { return false; }
    int return_value(int) { return 0; }
    bool await_ready() { return true; }
    bool await_suspend(std::experimental::coroutine_handle<B>) { return true; }
};
class A {
public:
    void* operator new(size_t) { return nullptr; }
    void operator delete(void*) {}
    void* operator new[](size_t) { return nullptr; }
    void operator delete[](void*) {}
    A& operator=(const A&) { return *this; }
    A& operator>>(int) { return *this; }
    A& operator<<(int) { return *this; }
    bool operator!() { return true; }
    bool operator==(const A&) { return true; }
    bool operator!=(const A&) { return true; }
    int operator[](int) { return 0; }
    operator long() { return 0L; }
    B* operator->() { return nullptr; }
    B operator*() { return B(); }
    A& operator++() { return *this; }
    A& operator++(int) { return *this; }
    A& operator--() { return *this; }
    A& operator--(int) { return *this; }
    A operator-(const A&) { return A(); }
    A operator+(const A&) { return A(); }
    A operator*(const A&) { return A(); }
    A operator/(const A&) { return A(); }
    A* operator&() { return nullptr; }
    B operator->*(int) { return B(); }
    A operator%(const A&) { return A(); }
    bool operator<(const A&) { return true; }
    bool operator>(const A&) { return true; }
    bool operator<=(const A&) { return true; }
    bool operator>=(const A&) { return true; }
    bool operator,(int) { return true; }
    B operator()() { return B(); }
    A operator~() { return A(); }
    A operator|(const A&) { return A(); }
    bool operator && (const A&) { return true; }
    bool operator || (const A&) { return true; }
    A& operator*=(const A&) { return *this; }
    A& operator+=(const A&) { return *this; }
    A& operator-=(const A&) { return *this; }
    A& operator/=(const A&) { return *this; }
    A& operator%=(const A&) { return *this; }
    A& operator>>=(const A&) { return *this; }
    A& operator<<=(const A&) { return *this; }
    A& operator&=(const A&) { return *this; }
    A& operator|=(const A&) { return *this; }
    A& operator^=(const A&) { return *this; }
    bool operator<=>(const A&) { return true; }
    B operator co_await() { return B(); }
};
template <>class std::experimental::coroutine_traits<int> {
public:
    using promise_type = B;
};
int func() {
    A a1, a2;
    A *pa = new A, *pb = new A[5];
    delete pa;
    delete[] pb;
    a1 = a2;
    a1 >> 2 << 3;
    if (!a1 && a1 == a2 && a1 != a2) {
        long l = a1;
        int i = a1[1];
    }
    a1->~B();
    B b = *a1;
    ++a1;
    a1++;
    --a1;
    a1--;
    a1 + a2 - a2 * a2 / a2 % a2 | a1;
    A* pa2 = &a1;
    a2->*3;
    if (a1 > a2 && a1 < a2 && a1 >= a2 && a1 <= a2) {
        a1, 2;
        a1();
        ~a1;
    }
    bool bb = (a1&& a2) && (a1|| a2);
    a1 += a2 -= a2 *= a2 /= a2 %= a2 <<= a2 >>= a2 &= a2 |= a2 ^= a2;
    a1 <=> a2;
    co_await a1;
    co_return 2;
}