//
// Created by jwc on 7/19/24.
//

#ifndef CLIPBOARD_H
#define CLIPBOARD_H

#include "DataInterchange.h"

#ifdef __cplusplus
extern "C" {
#endif
    _EXPORT_ P_INSTANCE(DataInterchange)  Clipboard_Paste();
    _EXPORT_ void Clipboard_Copy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);
    _EXPORT_ void Clipboard_Copy_WithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data, utf8_string_const format));
    _EXPORT_ void Clipboard_Copy_Persist(P_INSTANCE(DataInterchange) data);

    _EXPORT_ void Clipboard_Clear();
#ifdef __cplusplus
}
#endif



#endif //CLIPBOARD_H
