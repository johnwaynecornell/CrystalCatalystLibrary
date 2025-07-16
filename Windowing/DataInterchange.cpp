// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    utf8_string_struct BultinDataTypes[] = {"Text", "Uris", "Image", nullptr};

    P_INSTANCE(DataInterchange)  DataInterchange_Create() {
        return new DataInterchange();
    }

    void clear_selection(P_INSTANCE(DataInterchange) drag) {
        drag->selected_format = nullptr;

        if (drag->selected_data != nullptr) {
            free(drag->selected_data);
            drag->selected_data = nullptr;
        }

        drag->selected_size = 0;
    }

    void DataInterchange_Free(P_INSTANCE(DataInterchange) drag) {
        P_INSTANCE(DataInterchange::Node) I;
        for (I = DataInterchange_FormatEnum(drag); I != nullptr; I = I->next) {
            DataInterchange_ItemsFormatRemove(drag, I);
        }
        clear_selection(drag);
        delete drag;
    }

    bool DataInterchange_FormatExists(P_INSTANCE(DataInterchange) data, utf8_string_struct format) {
        std::string f = format.c_str;

        for (P_INSTANCE(DataInterchange::Node) n = data->data_head.next; n != nullptr; n = n->next) {
            if (f == n->type.c_str) return true;
        }
        return false;
    }


    P_INSTANCE(DataInterchange::Node)  DataInterchange_FormatAdd(P_INSTANCE(DataInterchange) drop, utf8_string_struct format) {
        if (strcmp(format, "text/uri=list") == 0) __asm("int3");

        P_INSTANCE(DataInterchange::Node) N = &drop->data_head;
        while (N->next != nullptr) N = N->next;

        N = N->next = new DataInterchange::Node();
        N->type = format;

        return N;
    }

    P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnum(P_INSTANCE(DataInterchange) drop) {
        return drop->data_head.next;
    }

    P_INSTANCE(DataInterchange::Node) DataInterchange_FormatEnumNext(P_INSTANCE(DataInterchange::Node) node) {
        return node->next;
    }

    void DataInterchange_FormatEnumText(P_INSTANCE(DataInterchange::Node) node, P_OUT(utf8_string_struct) text) {
        *text = node->type;
    }

    P_INSTANCE(DataInterchange::Node) DataInterchange_ItemsFormatRemove(P_INSTANCE(DataInterchange) drop, P_INSTANCE(DataInterchange::Node) node) {
        P_INSTANCE(DataInterchange::Node) I;

        for (I = DataInterchange_FormatEnum(drop); I != nullptr && I->next != node; I = I->next){}

        if (I == nullptr) return nullptr;

        P_INSTANCE(DataInterchange::Node) R = I->next;

        I->next = R->next;

        return I->next;
    }

    void DataInterchange_SelectionReveal(P_INSTANCE(DataInterchange) drag, P_OUT(utf8_string_struct) format, P_OUT(P_INSTANCE(void)) data, P_OUT(size_t) size)
    {
        if (format) *format = drag->selected_format;
        if (data) *data = drag->selected_data;
        if (size) *size = drag->selected_size;
    }

    void DataInterchange_SelectionSet(P_INSTANCE(DataInterchange) drag, utf8_string_struct format, P_INSTANCE(void) data, size_t size) {

        drag->selected_format = format;

        drag->selected_size = size;

        if (drag->selected_data != nullptr) {
            free(drag->selected_data);
            drag->selected_data = nullptr;
        }

        drag->selected_data = malloc(size+4);

        int32_t index=0;

        for (index=0; index < size; index++) static_cast<P_ELEMENTS(uint8_t) >(drag->selected_data)[index] = static_cast<P_ELEMENTS(uint8_t) >(data)[index];
        for (int32_t i=0; i<4; i++) static_cast<P_ELEMENTS(uint8_t) >(drag->selected_data)[index++] = 0;

    }
}