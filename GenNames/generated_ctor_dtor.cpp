// vector deleting destructor (called from vftable / directedly)
// default constructor closure
// scalar deleting destructor (called by delete / explicit destructor call)
// eh vector constructor iterator
// eh vector destructor iterator
// eh vector vbase constructor iterator
// eh vector copy constructor iterator
// eh vector vbase copy constructor iterator

class vbase {
public: int x;
      vbase(int k = 0) :x(k) {}
      virtual void vfoo() {}
      virtual void vbar() {}
      virtual ~vbase() {}
};

class one : virtual public vbase {
public:
    one() = default;
    ~one() = default;
    one(const one&) {}
};

class wrap {
public:
    vbase v[7];
    one o[11];
};

class two {
public:
    two() {}
    ~two() {}
};

void generated_ctor_dtor() {
    auto pv = new vbase[13];
    delete[] pv;
    auto pone = new one[17];
    delete[] pone;
    auto ptwo = new two;
    delete ptwo;
    ptwo = new two[15];
    delete[] ptwo;
    two tt;
    tt.~two();

    wrap w;
    wrap v = w;
}