class C {
public:
    C() {}
    ~C() {}
};

class D {
public:
    static C d_c;
};

C D::d_c;

auto lambda = []() {
    static C local_c_in_lambda;
};

class E {
public:
    const E(const C*) {}
    ~E() {}
};

inline void inline_func() {
    static const E inline_e(&D::d_c);
}

void dynamic_init_dtor() {
    thread_local C local_c;
    C local_c_3 = D::d_c;
    lambda();
    inline_func();
}

int main(int argc, char** argv) {
    static C local_c_in_main;
    return 0;
}

extern "C" void c_function() {
    static C local_c_in_c_function;
}


