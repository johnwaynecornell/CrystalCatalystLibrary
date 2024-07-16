#ifndef ERRORSYSTEM_H
#define ERRORSYSTEM_H
#include "CrystalCatalystLibrary.h"
#include "Under/utf8_string.h"

struct ErrorKeyValue {
    utf8_string_const key;
    utf8_string_const name;
    utf8_string_const value;
};

struct ErrorInfo {
    P_ELEMENTS(const ErrorKeyValue) details; // List of key-name-value triples
    size_t detail_count;

    utf8_string_const message;           // Error message

    ErrorInfo(P_ELEMENTS(const ErrorKeyValue) details, size_t detail_count, utf8_string_const msg);
    ~ErrorInfo();
};

extern "C"
{
_EXPORT_ void LogError(P_ELEMENTS(const ErrorKeyValue)  details, size_t detail_count, utf8_string_const message);
_EXPORT_ void ClearErrors();
}

void _LogError(P_ELEMENTS(const ErrorKeyValue)  details, utf8_string_const message);


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

#endif //ERRORSYSTEM_H
