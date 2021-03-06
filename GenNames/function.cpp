using nullptr_t = decltype(nullptr);

// no parameter
void func() {};

// parameter list
int func(int, char, class A&, const double, nullptr_t, nullptr_t) { return 0; }

// ellipse
int&& func(...) { return 1; }
void func(const char*, ...) {}

// exception specifier
// exception specifiers currently ignored
void func(unsigned int) noexcept(true) {}
void func(int) noexcept(false) {}
void func(short) throw(int, short) {}

// static function
static void func(char) {}

void dummy_func() {
    func('c');
}

// call conv
void __fastcall func_fast() {}
void __vectorcall func_vec() {}
void __stdcall func_std() {}
void __cdecl func_cdecl() {}

// function pointer
void (*pfunc)();