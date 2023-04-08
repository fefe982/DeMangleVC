ref class RA {
public:
    property RA^ simple_char;
};
RA a;
RA^ pa = % a;
void foo() {
    RA a;
    RA^ c = a.simple_char;
}
