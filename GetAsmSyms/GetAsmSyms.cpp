#include <string>
#include <fstream>
#include <iostream>
#include <regex>

std::regex regexPROC("(\\?[^ \\t]+) PROC");
std::regex regexVAR("(\\?[^ \\t]+) D");

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
                std::smatch sm;
                if (std::regex_search(line, sm, regexPROC, std::regex_constants::match_continuous))
                {
                    std::cout << sm[1] << std::endl;
                }
                else if (std::regex_search(line, sm, regexVAR, std::regex_constants::match_continuous))
                {
                    std::cout << sm[1] << std::endl;
                }
            }
        }
    }
}