using size_t = unsigned int;


template <size_t _Size>
constexpr size_t size(const char(&)[_Size]) noexcept {
    return _Size;
}


struct exampleStruct {
    virtual size_t example() {
        static constexpr size_t exampleLength = size("hello world") - 1;
        return exampleLength;
    }
};

void func() {
    static int i = 0;
    exampleStruct e;
    e.example();
}