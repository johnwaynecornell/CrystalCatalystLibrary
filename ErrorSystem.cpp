// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "include/CrystalCatalystLibrary/CrystalCatalystLibrary.h"

ErrorKeyValue CloneErrorKeyValue(ErrorKeyValue source) {
    ErrorKeyValue R;
    R.key = utf8_string_dup(source.key);
    R.name = utf8_string_dup(source.name);
    R.value = utf8_string_dup(source.value);
    return R;
}



ErrorInfo::ErrorInfo(P_ELEMENTS(const ErrorKeyValue) details, size_t detail_count, utf8_string_const msg) {
    this->detail_count = detail_count;
    this->details = new ErrorKeyValue[detail_count];

    for (int i=0; i<detail_count; i++) {
        ((P_ELEMENTS(ErrorKeyValue)) this->details)[i] = CloneErrorKeyValue(details[i]);
    }

    this->message = utf8_string_dup(msg);
}

ErrorInfo::~ErrorInfo() {

    for (int i=0; i<detail_count; i++) {
        delete[] this->details[i].key;
        delete[] this->details[i].name;
        delete[] this->details[i].value;
    }

    delete[] this->details;
    delete[] this->message;
}

P_INSTANCE(void) TLS_ERRORS_INITIALIZE_VALUE()
{
    return new SingleLink_Node<P_INSTANCE(ErrorInfo) >();
}

void TLS_ERRORS_DESTRUCTOR(P_INSTANCE(void) value)
{
    /* TODO */ //consider logging to stderr

    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) n;
    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) n2;

    for (n = static_cast<P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) >(value); n != nullptr; n = n2) {

        n2 = n->next;

        delete n->value;
        delete n;
    }
}

void ClearErrors() {
    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) n;
    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) n2;
    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) head =
        static_cast<P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) >(TheCrystalCatalystLibrary->errors->get());

    for (n = head->next; n != nullptr; n = n2) {

        n2 = n->next;

        if (n->value) delete n->value;
        delete n;
    }

    head->next = nullptr;
}


P_INSTANCE(TLS)  _initialize_errors_() {
    return TLS_Alloc(TLS_ERRORS_INITIALIZE_VALUE, TLS_ERRORS_DESTRUCTOR);
}

std::string AggregateErrors() {
    std::string aggregatedMessage;

    for (P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) node =
            ( (P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) )
        TheCrystalCatalystLibrary->errors->get())->next; node != nullptr; node = node->next) {
        P_INSTANCE(ErrorInfo)  current = node->value;

        for (int i=0; i < current->detail_count; i++) {

            ErrorKeyValue detail = current->details[i];
            aggregatedMessage += (std::string) detail.name + ":[" + detail.key + " = \"" + detail.value + "\"] ";

        }
        aggregatedMessage += (std::string) current->message + "\n";
    }

    return aggregatedMessage;
}

void LogError(P_ELEMENTS(const ErrorKeyValue)  details, size_t detail_count, utf8_string_const message) {
    P_INSTANCE(ErrorInfo) err = new ErrorInfo(details, detail_count, message);

    P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>)  l = ((P_INSTANCE(SingleLink_Node<P_INSTANCE(ErrorInfo)>) )
        TheCrystalCatalystLibrary->errors->get());

    l->tail_add(err);
}

void _LogError(P_ELEMENTS(const ErrorKeyValue)  details, utf8_string_const message) {
    size_t detail_count = 0;
    while (details[detail_count].key != nullptr) detail_count++;
    LogError(details, detail_count, message);
}
