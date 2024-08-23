#include <iostream>
#include "Clipboard_Windows.h"
#include "SimpleDataObject.h"

using namespace JWCEssentials;

namespace NewAge {

P_INSTANCE(DataInterchange)  Clipboard_Paste(P_INSTANCE(WindowHandle) handle)
{
    IDataObject* pDataObject = nullptr;
    HRESULT hr = OleGetClipboard(&pDataObject);
    if (FAILED(hr)) {
        std::cerr << "Failed to get clipboard data. HRESULT: " << std::hex << hr << std::endl;
    }

    P_INSTANCE(DataInterchange) data = DataInterchange_Create();
	data->selection_type = DataInterchange::E_CLIPBOARD;
	data->m_handle = handle;

    data->context = pDataObject;
    hr= DataInterchange_ReadFormats(data, pDataObject);

    if (FAILED(hr)) {
        std::cerr << "Failed to get clipboard formats. HRESULT: " << std::hex << hr << std::endl;
    }

    return data;
}

void Clipboard_Copy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data)
{
    DataInterchange_CreateContext(data);
    data->m_handle = handle;
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        std::cerr << "Failed to set clipboard data. HRESULT: " << std::hex << hr << std::endl;
    }

    pDataObject->Release();

    data->provide_chosen = DataInterchange::provide_for_clipboard;
}

void Clipboard_Copy_WithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data, utf8_string_struct format), P_INSTANCE(DataInterchange)  data)
{
    DataInterchange_CreateContext(data);
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        std::cerr << "Failed to set clipboard data. HRESULT: " << std::hex << hr << std::endl;
    }

    pDataObject->Release();

    data->provide_chosen = provide;
}

void Clipboard_Copy_Persist(P_INSTANCE(DataInterchange) dataInterchange) {
    if (!OpenClipboard(nullptr)) {
        std::cerr << "Failed to open clipboard." << std::endl;
        return;
    }

	dataInterchange->selection_type = DataInterchange::E_CLIPBOARD;

    EmptyClipboard();

    for (DataInterchange::Node *node = dataInterchange->data_head.next; node != nullptr; node = node->next) {

        UINT cfFormat;
        HGLOBAL hGlobal;

        hGlobal = DataInterchange_MakeHGLOBAl(dataInterchange, node->type, &cfFormat);
        if (hGlobal) {
            SetClipboardData(cfFormat, hGlobal);
        } else {
            std::cerr << "Failed to allocate global memory." << std::endl;
        }
    }

    CloseClipboard();
}

void Clipboard_Clear()
{
    if (!OpenClipboard(nullptr)) {
        std::cerr << "Failed to open clipboard." << std::endl;
        return;
    }
    EmptyClipboard();
    CloseClipboard();
}
}