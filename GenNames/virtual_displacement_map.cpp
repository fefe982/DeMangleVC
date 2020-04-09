// virtual displacement map

struct A { virtual void f() {}; };
struct B : virtual A {};
struct C : virtual B, virtual A {};
typedef void (B::* Bpmf)();
typedef void (C::* Cpmf)();
extern Cpmf out;
extern Bpmf in;
void cvt_pmf() { out = in; }