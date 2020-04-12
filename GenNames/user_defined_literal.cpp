const char* operator ""_a(const char* str) { return str; }
const char* operator "" _b(const char* str) { return str; }
template <char...> double operator "" _x() {
    return 0.0;
}

auto pa = 123_a;
auto pb = 456_b;
auto pc = 123_x;
