class C {};

class MyClass {

public:
    MyClass() { }
    // Placement new operator
    void* operator new (size_t sz, C) {
        return nullptr;
    }

    void operator delete (void*, C) {

    }

    ~MyClass() {
        // Cleanup
    }
};

int main()
{
    C c;
    auto obj = new(c)MyClass;
    obj->~MyClass();
}