//
// Created by jwc on 7/19/24.
//

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    void DataInterchange::provide_for_clipboard(P_INSTANCE(DataInterchange) data, utf8_string_struct format)
    {
        DragDropData *dta = (DragDropData*)data;
        dta->m_handle->crystal_window->callbacks.on_clipboard_provide_chosen(dta->m_handle, dta, format);

    }

    void handleDataInterchangeError(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) di, std::string message)
    {
        if (handle && handle->crystal_window && handle->crystal_window->callbacks.on_data_interchange_error) handle->crystal_window->callbacks.on_data_interchange_error(handle, di, message.c_str());
        else std::cerr << "Error: " << message << std::endl;
    }
}