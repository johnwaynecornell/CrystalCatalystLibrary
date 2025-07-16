// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#ifndef CRYSTALCATALYST_DATAINTERCHANGE_H
#define CRYSTALCATALYST_DATAINTERCHANGE_H

using namespace JWCEssentials;

namespace NewAge {
    class WindowHandle;

    class DataInterchange {
    public:
        void *context=nullptr;
        void *arg= nullptr;

        class Node {
        public:
            utf8_string_struct type;
            Node *next;
        };

        enum ESELECTION {
            E_CLIPBOARD = 1,
            E_DND = 2
        };

        ESELECTION selection_type;

        utf8_string_struct selected_format;
        P_INSTANCE(void) selected_data;
        size_t selected_size;

        Node data_head;

        WindowHandle *m_handle = nullptr;

        void (*provide_chosen)(P_INSTANCE(DataInterchange) data, utf8_string_struct format) = nullptr;
        static void provide_for_drag(P_INSTANCE(DataInterchange) data, utf8_string_struct format);
        static void provide_for_clipboard(P_INSTANCE(DataInterchange) data, utf8_string_struct format);
    };

    _EXPORT_ P_INSTANCE(DataInterchange)  DataInterchange_Create();
    _EXPORT_ void DataInterchange_Free(P_INSTANCE(DataInterchange) drag);

    _EXPORT_ P_INSTANCE(DataInterchange::Node)  DataInterchange_FormatAdd(P_INSTANCE(DataInterchange) drop, utf8_string_struct format);
    _EXPORT_ bool DataInterchange_FormatExists(P_INSTANCE(DataInterchange) data, utf8_string_struct format);
    _EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnum(P_INSTANCE(DataInterchange) drop);
    _EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnumNext(P_INSTANCE(DataInterchange::Node) node);
    _EXPORT_ void DataInterchange_FormatEnumText(P_INSTANCE(DataInterchange::Node) node, P_OUT(utf8_string_struct) text);

    _EXPORT_ P_INSTANCE(DataInterchange::Node) DataInterchange_ItemsFormatRemove(P_INSTANCE(DataInterchange) drop, P_INSTANCE(DataInterchange::Node) node);

    _EXPORT_ void DataInterchange_Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format);
    _EXPORT_ void DataInterchange_SelectionReveal(P_INSTANCE(DataInterchange) drag, P_OUT(utf8_string_struct) format, P_OUT(P_INSTANCE(void)) data, P_OUT(size_t) size);
    _EXPORT_ void DataInterchange_SelectionSet(P_INSTANCE(DataInterchange) drag, utf8_string_struct format, P_INSTANCE(void) data, size_t size);
}

#endif //CRYSTALCATALYST_DATAINTERCHANGE_H
