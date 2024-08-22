//
// Created by jwc on 7/19/24.
//

#include "../include/CrystalCatalystLibrary/CrystalCatalystLibrary.h"


void DataInterchange::provide_for_clipboard(P_INSTANCE(DataInterchange) data, utf8_string_const format)
{
    DragDropData *dta = (DragDropData*)data;
    dta->m_handle->crystal_window->callbacks.on_clipboard_provide_chosen(dta->m_handle, dta, format);

}

