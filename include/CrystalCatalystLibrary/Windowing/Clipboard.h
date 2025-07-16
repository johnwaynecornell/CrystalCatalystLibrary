//
// Created by jwc on 7/19/24.
//

#ifndef CLIPBOARD_H
#define CLIPBOARD_H

#include "DataInterchange.h"

namespace NewAge {
    _EXPORT_ P_INSTANCE(DataInterchange)  CrystalWindow_ClipboardPaste(P_INSTANCE(WindowHandle) handle);
    _EXPORT_ void CrystalWindow_ClipboardCopy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);
    _EXPORT_ void CrystalWindow_ClipboardCopyWithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data), P_INSTANCE(DataInterchange) data);
    _EXPORT_ void CrystalWindow_ClipboardCopyPersist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);

    _EXPORT_ void CrystalWindow_ClipboardClear();
}


#endif //CLIPBOARD_H
