#include <string>
#include <fstream>
#include <iostream>
#include <regex>
#include <map>
#include <filesystem>

std::regex regexSymbol(R"(^(?:[^;]*\s)?(\?[^\s:,]+)[\s:].*?(?:;\s*(.+))?$)");

int main(int argc, char **argv)
{
    if (argc <= 1) {
        return 0;
    }
    std::filesystem::path p(argv[1]);
    std::vector<std::string> file_paths;
    if (std::filesystem::is_regular_file(p)) {
        file_paths.push_back(argv[1]);
    }
    else if (std::filesystem::is_directory(p)) {
        for (auto& f : std::filesystem::directory_iterator(p)) {
            if (f.is_regular_file()) {
                std::string s_file = f.path().string();
                std::string s_ext = f.path().extension().string();
                std::transform(s_ext.begin(), s_ext.end(), s_ext.begin(), [](unsigned char c) {return std::tolower(c); });
                if (s_ext == ".asm") {
                    file_paths.push_back(s_file);
                }
            }
        }
    }
    for (const auto &f: file_paths)
    {
        std::ifstream isAsm;
        isAsm.open(f);
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