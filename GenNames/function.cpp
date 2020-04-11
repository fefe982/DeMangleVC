
// no parameter
void func() {};

// parameter list
int func(int, char, class A&, const double) { return 0; }

// ellipse
int&& func(...) { return 1; }
void func(const char*, ...) {}

// exception specifier
// exception specifiers currently ignored
void func(unsigned int) noexcept {}
void func(int) noexcept(false) {}
void func(short) throw(int, short) {}

// static function
static void func(char) {}

void dummy_func() {
    func('c');
}