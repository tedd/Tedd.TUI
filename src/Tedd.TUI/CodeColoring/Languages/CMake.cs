using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring.Languages;

public class CMakeLanguage : ILanguage
{
    public string Id => "cmake";
    public string[] Aliases => [];

    public Grammar GetGrammar()
    {
        var grammar = new Grammar();
        grammar.Add("comment", new Pattern(@"#.*"));

        var interpolationInside = new Grammar();
        interpolationInside.Add("punctuation", new Pattern(@"\$\{|\}"));
        interpolationInside.Add("variable", new Pattern(@"\w+"));

        var stringInside = new Grammar();
        stringInside.Add("interpolation", new Pattern(@"\$\{(?:[^{}$]|\$\{[^{}$]*\})*\}", inside: interpolationInside));

        grammar.Add("string", new Pattern(@"""(?:[^\\""]|\\.)*""", greedy: true, inside: stringInside));
        grammar.Add("variable", new Pattern(@"\b(?:CMAKE_\w+|\w+_(?:(?:BINARY|SOURCE)_DIR|DESCRIPTION|HOMEPAGE_URL|ROOT|VERSION(?:_MAJOR|_MINOR|_PATCH|_TWEAK)?)|(?:ANDROID|APPLE|BORLAND|BUILD_SHARED_LIBS|CACHE|CYGWIN|ENV|EXECUTABLE_OUTPUT_PATH|GHS-MULTI|IOS|LIBRARY_OUTPUT_PATH|MINGW|MSVC(?:10|11|12|14|60|70|71|80|90|_IDE|_TOOLSET_VERSION|_VERSION)?|MSYS|PROJECT_NAME|UNIX|WIN32|WINCE|WINDOWS_PHONE|WINDOWS_STORE|XCODE))\b"));
        grammar.Add("property", new Pattern(@"\b(?:cxx_\w+|(?:ARCHIVE_OUTPUT_(?:DIRECTORY|NAME)|COMPILE_DEFINITIONS|COMPILE_PDB_NAME|COMPILE_PDB_OUTPUT_DIRECTORY|EXCLUDE_FROM_DEFAULT_BUILD|IMPORTED_(?:IMPLIB|LIBNAME|LINK_DEPENDENT_LIBRARIES|LINK_INTERFACE_LANGUAGES|LINK_INTERFACE_LIBRARIES|LINK_INTERFACE_MULTIPLICITY|LOCATION|NO_SONAME|OBJECTS|SONAME)|INTERPROCEDURAL_OPTIMIZATION|LIBRARY_OUTPUT_DIRECTORY|LIBRARY_OUTPUT_NAME|LINK_FLAGS|LINK_INTERFACE_LIBRARIES|LINK_INTERFACE_MULTIPLICITY|LOCATION|MAP_IMPORTED_CONFIG|OSX_ARCHITECTURES|OUTPUT_NAME|PDB_NAME|PDB_OUTPUT_DIRECTORY|RUNTIME_OUTPUT_DIRECTORY|RUNTIME_OUTPUT_NAME|STATIC_LIBRARY_FLAGS|VS_CSHARP|VS_DOTNET_REFERENCEPROP|VS_DOTNET_REFERENCE|VS_GLOBAL_SECTION_POST|VS_GLOBAL_SECTION_PRE|VS_GLOBAL|XCODE_ATTRIBUTE)_\w+|\w+_(?:CLANG_TIDY|COMPILER_LAUNCHER|CPPCHECK|CPPLINT|INCLUDE_WHAT_YOU_USE|OUTPUT_NAME|POSTFIX|VISIBILITY_PRESET)|INCLUDE_DIRECTORIES|INSTALL_RPATH|LABELS|LANGUAGE|LINKER_LANGUAGE|POSITION_INDEPENDENT_CODE|SOURCES|TIMEOUT|TYPE|VERSION|WORKING_DIRECTORY)\b"));
        grammar.Add("keyword", new Pattern(@"\b(?:add_compile_definitions|add_compile_options|add_custom_command|add_custom_target|add_definitions|add_dependencies|add_executable|add_library|add_link_options|add_subdirectory|add_test|aux_source_directory|break|build_command|build_name|cmake_host_system_information|cmake_minimum_required|cmake_parse_arguments|cmake_policy|configure_file|continue|create_test_sourcelist|ctest_build|ctest_configure|ctest_coverage|ctest_empty_binary_directory|ctest_memcheck|ctest_read_custom_files|ctest_run_script|ctest_sleep|ctest_start|ctest_submit|ctest_test|ctest_update|ctest_upload|define_property|else|elseif|enable_language|enable_testing|endforeach|endfunction|endif|endmacro|endwhile|exec_program|execute_process|export|export_library_dependencies|file|find_file|find_library|find_package|find_path|find_program|fltk_wrap_ui|foreach|function|get_cmake_property|get_directory_property|get_filename_component|get_property|get_source_file_property|get_target_property|get_test_property|if|include|include_directories|include_external_msproject|include_guard|include_regular_expression|install|install_files|install_programs|install_targets|link_directories|link_libraries|list|load_cache|load_command|macro|make_directory|mark_as_advanced|math|message|option|output_required_files|project|qt_wrap_cpp|qt_wrap_ui|remove|remove_definitions|return|separate_arguments|set|set_directory_properties|set_property|set_source_files_properties|set_target_properties|set_tests_properties|site_name|source_group|string|subdir_depends|subdirs|target_compile_definitions|target_compile_features|target_compile_options|target_include_directories|target_link_directories|target_link_libraries|target_link_options|target_sources|try_compile|try_run|unset|use_mangled_mesa|utility_source|variable_requires|variable_watch|while|write_file)(?=\s*\()\b"));
        grammar.Add("boolean", new Pattern(@"\b(?:FALSE|OFF|ON|TRUE)\b"));
        grammar.Add("namespace", new Pattern(@"\b(?:INTERFACE|PRIVATE|PROPERTIES|PUBLIC|SHARED|STATIC|TARGET_OBJECTS)\b"));
        grammar.Add("operator", new Pattern(@"\b(?:AND|DEFINED|EQUAL|GREATER|LESS|MATCHES|NOT|OR|STREQUAL|STRGREATER|STRLESS|VERSION_EQUAL|VERSION_GREATER|VERSION_LESS)\b"));
        grammar.Add("inserted", new Pattern(@"\b\w+::\w+\b", alias: "class-name"));
        grammar.Add("number", new Pattern(@"\b\d+(?:\.\d+)*\b"));
        grammar.Add("function", new Pattern(@"\b[a-z_]\w*(?=\s*\()\b", regexOptions: "i"));
        grammar.Add("punctuation", new Pattern(@"[()>}]|\$[<{]"));
        return grammar;
    }
}
