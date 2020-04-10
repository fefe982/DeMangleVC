#include <iostream>

class C {
public:
    C() {
        std::cout << "C ctor";
    }
    ~C() {
        std::cout << "C dtor";
    }
};

class D {
public:
    static C d_c;
};

C D::d_c;

void dynamic_init_dtor() {
    thread_local C local_c;
    C local_c_3 = D::d_c;
}