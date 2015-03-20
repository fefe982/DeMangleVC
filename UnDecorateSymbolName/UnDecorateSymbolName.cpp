#include "windows.h"
#include "dbghelp.h"
#include <fstream>
#include <string>
#include <iostream>
using std::ifstream;
using std::cout;
using std::string;
using std::getline;
using std::endl;

#pragma comment(lib, "dbghelp.lib")

int main(int argc, char **argv)
{
    //char bufin[2048];
    char bufout[8192];

    if(argc <= 1)
    {
        return 1;
    }

    string bufin;
    ifstream filein(argv[1]);

    while(getline(filein, bufin))
    {
        std::string::size_type pos = bufin.find(" ");
        bufin = bufin.substr(0, pos);
        DWORD ret = UnDecorateSymbolName(bufin.c_str(), bufout, 8192, 0);
        if(ret)
        {
            cout << bufout << endl;
        }
        else
        {
            DWORD err = GetLastError();
            cout << "?EE? " << err << endl;
        }
    }

    return 0;
}