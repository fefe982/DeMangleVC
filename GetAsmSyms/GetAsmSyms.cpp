#include <string>
#include <fstream>
#include <iostream>
#include <boost/regex.hpp>

boost::regex regexPROC("(\\?[^ \\t]+) PROC");
boost::regex regexVAR("(\\?[^ \\t]+) D");

int main(int argc, char **argv)
{
    if (argc > 1)
    {
        std::ifstream isAsm;
        isAsm.open(argv[1]);
        if (isAsm)
        {
            std::string line;
            while (std::getline(isAsm, line))
            {
                boost::smatch sm;
                if (boost::regex_search(line, sm, regexPROC, boost::regex_constants::match_continuous))
                {
                    std::cout << sm[1] << std::endl;
                }
                else if (boost::regex_search(line, sm, regexVAR, boost::regex_constants::match_continuous))
                {
                    std::cout << sm[1] << std::endl;
                }
            }
        }
    }
}