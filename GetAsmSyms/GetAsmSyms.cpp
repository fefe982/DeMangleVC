#include <string>
#include <fstream>
#include <iostream>
#include <regex>
#include <map>

std::regex regexSymbol(R"((?:^|\s)(\?[^\s:,]+)[\s:].*?(?:;\s*(.+))?$)");

int main(int argc, char **argv)
{
    if (argc > 1)
    {
        std::ifstream isAsm;
        isAsm.open(argv[1]);
        if (isAsm)
        {
            std::string line;
            std::map<std::string, std::string> sym_map;
            while (std::getline(isAsm, line))
            {
                std::smatch sm;
                if (std::regex_search(line, sm, regexSymbol))
                {
                    if (sm.size() == 3)
                    {
                        sym_map[sm[1]] = sm[2];
                    }
                    else
                    {
                        sym_map[sm[1]];
                    }
                }
            }
            for (auto const & p : sym_map)
            {
                std::cout << p.first << " " << p.second << "\n";
            }
        }
    }
}