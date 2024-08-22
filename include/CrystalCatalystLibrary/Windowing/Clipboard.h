//
// Created by jwc on 7/19/24.
//

#ifndef CLIPBOARD_H
#define CLIPBOARD_H

#include "DataInterchange.h"

namespace NewAge {
    _EXPORT_ P_INSTANCE(DataInterchange)  Clipboard_Paste(P_INSTANCE(WindowHandle) handle);
    _EXPORT_ void Clipboard_Copy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);
    _EXPORT_ void Clipboard_Copy_WithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data), P_INSTANCE(DataInterchange) data);
    _EXPORT_ void Clipboard_Copy_Persist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);

    _EXPORT_ void Clipboard_Clear();
}


#endif //CLIPBOARD_H
