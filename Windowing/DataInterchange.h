// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#ifndef CRYSTALCATALYST_DATAINTERCHANGE_H
#define CRYSTALCATALYST_DATAINTERCHANGE_H

class WindowHandle;

class DataInterchange {
public:
    void *context=nullptr;
    void *arg= nullptr;

    class Node {
    public:
        utf8_string_const type;
        Node *next;
    };

    enum ESELECTION {
        E_CLIPBOARD = 1,
        E_DND = 2
    };

    ESELECTION selection_type;

    utf8_string_const selected_format;
    P_INSTANCE(void) selected_data;
    size_t selected_size;

    Node data_head;

    WindowHandle *m_handle = nullptr;

    void (*provide_chosen)(P_INSTANCE(DataInterchange) data, utf8_string_const format) = nullptr;
    static void provide_for_drag(P_INSTANCE(DataInterchange) data, utf8_string_const format);
    static void provide_for_clipboard(P_INSTANCE(DataInterchange) data, utf8_string_const format);
};

#ifdef __cplusplus
extern "C" {
#endif

_EXPORT_ P_INSTANCE(DataInterchange)  DataInterchange_Create();
_EXPORT_ void DataInterchange_Free(P_INSTANCE(DataInterchange) drag);

_EXPORT_ P_INSTANCE(DataInterchange::Node)  DataInterchange_FormatAdd(P_INSTANCE(DataInterchange) drop, utf8_string_const format);
_EXPORT_ bool DataInterchange_FormatExists(P_INSTANCE(DataInterchange) data, utf8_string_const format);
_EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnum(P_INSTANCE(DataInterchange) drop);
_EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnum_Next(P_INSTANCE(DataInterchange::Node) node);
_EXPORT_ void DataInterchange_FormatEnum_Text(P_INSTANCE(DataInterchange::Node) node, P_OUT(utf8_string_const) text);

_EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_Items_FormatRemove(P_INSTANCE(DataInterchange) drop, P_INSTANCE(DataInterchange::Node) node);

_EXPORT_ void DataInterchange_Select(P_INSTANCE(DataInterchange) data, utf8_string_const format);
_EXPORT_ void DataInterchange_Selection_Reveal(P_INSTANCE(DataInterchange) drag, P_OUT(utf8_string_const) format, P_OUT(P_INSTANCE(void)) data, P_OUT(size_t) size);
_EXPORT_ void DataInterchange_Selection_Set(P_INSTANCE(DataInterchange) drag, utf8_string_const format, P_INSTANCE(void) data, size_t size);

#ifdef __cplusplus
}
#endif

#endif //CRYSTALCATALYST_DATAINTERCHANGE_H
