// vftable
// vbtable
// vcall
// vbase destructor
// vector deleting destructor (used in vftable)
// vector deleting destructor [thunk] (used in vftable)
// scalar deleting destructor (generated but not referenced)
// eh vector destructor iterator
// eh vector vbase constructor iterator
// RTTI Type Descriptor
// RTTI Base Class Descriptor
// RTTI Base Class Array
// RTTI Class Hierarchy Descriptor
// RTTI Complete Object Locator

class vbase {
public: int x;
      vbase(int k = 0) :x(k) {}
      virtual void vfoo() { }
      virtual void vbar() { }
      virtual ~vbase() { }
};

class one : virtual public  vbase
{
public:
    int _a;
    virtual void vfoo() { }
    virtual void afoo() { }
};
class two : virtual public  vbase
{
public:
    int _b;
    virtual void vfoo() { }
    virtual void bfoo() { }
};
class three : public one, public two, virtual public vbase
{
public:
    int c;
    virtual int foo() {
        return 0;
    }
    virtual void vfoo() { }
    ~three() { }
};

class four : public three {
public:
    int d;
    virtual void vfoo() { }
};

void f(void(four::*)()) {}

void vftable_vbtable() {
    four* pf = new four[11];
    delete[] pf;
    pf = new four;
    delete pf;
    f(&four::vfoo);
}