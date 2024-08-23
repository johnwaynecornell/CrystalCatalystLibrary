// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef ERRORSYSTEM_H
#define ERRORSYSTEM_H

using namespace JWCEssentials;

namespace NewAge {
    struct ErrorKeyValue {
        utf8_string_struct key;
        utf8_string_struct name;
        utf8_string_struct value;
    };

    struct ErrorInfo {
        P_ELEMENTS(ErrorKeyValue) details; // List of key-name-value triples
        size_t detail_count;

        utf8_string_struct message;           // Error message

        ErrorInfo(P_ELEMENTS(const ErrorKeyValue) details, size_t detail_count, utf8_string_struct msg);
        ~ErrorInfo();
    };

    extern "C"
    {
        _EXPORT_ void LogError(P_ELEMENTS(const ErrorKeyValue)  details, size_t detail_count, utf8_string_struct message);
        _EXPORT_ void ClearErrors();
    }

    void _LogError(P_ELEMENTS(const ErrorKeyValue)  details, utf8_string_struct message);


    /* std::vector<KeyValue> errorDetails = {
    {"module", "CrystalCatalyst.DragDrop.X11", "void CrystalWindow_X11::RegisterDragTarget()"},
    {"facility", "Windowing", "0x00000000"},
    {"category", "unable to perform", "RegisterDragTarget"},
    {nullptr}
    };

    LogError(errorDetails, "Failed to register drag target.");



    Facilities
    const std::string Facility_Windowing = "Windowing";
    const std::string Facility_Input = "Input";
    const std::string Facility_Networking = "Networking";
    // Add more facilities as needed

    Categories
    const std::string Category_Performance = "Performance";
    const std::string Category_Initialization = "Initialization";
    const std::string Category_ResourceManagement = "ResourceManagement";
    // Add more categories as needed*/
}
#endif //ERRORSYSTEM_H
