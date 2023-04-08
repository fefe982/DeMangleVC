ref class RA {
internal:
    property RA^ index_ra[int] {
        RA^ get(int i) {
            return this;
        }
        void set(int i, RA^ ra) {}
    }

public:
    property virtual RA^ simple_ra;
    property RA^ normal_ra {
        RA^ get() {
            return this->index_ra[0];
        }
        void set(RA^ ra) {
            this->index_ra[0] = ra;
        }
    }
    property RA^ static_ra {
        static RA^ get() {
            static RA a;
            return %a;
        }
        static void set(RA^ ra) {}
    }
};
RA a;
RA^ pa = % a;
void foo() {
    RA a;
    RA^ c = a.simple_ra;
    a.simple_ra = c;
    c = a.normal_ra;
    a.normal_ra = c;
    c = a.static_ra;
    a.static_ra = c;
}
