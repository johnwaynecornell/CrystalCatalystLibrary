#include <iostream>
#include "Clipboard_Windows.h"
#include "SimpleDataObject.h"

using namespace JWCEssentials;

namespace NewAge {

P_INSTANCE(DataInterchange)  CrystalWindow_ClipboardPaste(P_INSTANCE(WindowHandle) handle)
{
    P_INSTANCE(DataInterchange) data = DataInterchange_Create();
	data->selection_type = DataInterchange::E_CLIPBOARD;
	data->m_handle = handle;

    IDataObject* pDataObject = nullptr;
    HRESULT hr = OleGetClipboard(&pDataObject);
    if (FAILED(hr)) {
        std::cerr << "Failed to get clipboard data. HRESULT: " << std::hex << hr << std::endl;
        return data;
    }

    data->context = pDataObject;
    hr = DataInterchange_ReadFormats(data, pDataObject);

    if (FAILED(hr)) {
        std::cerr << "Failed to get clipboard formats. HRESULT: " << std::hex << hr << std::endl;
    }

    return data;
}

void CrystalWindow_ClipboardCopy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data)
{
    DataInterchange_CreateContext(data);
    data->m_handle = handle;
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        if (hr == CO_E_NOTINITIALIZED) {
            std::cerr << "Failed to set clipboard data. COM/OLE not initialized on this thread. "
                         "Ensure Application_Init was called and the thread is STA." << std::endl;
        } else {
            std::cerr << "Failed to set clipboard data. HRESULT: " << std::hex << hr << std::endl;
        }
    }

    pDataObject->Release();

    data->provide_chosen = DataInterchange::provide_for_clipboard;
}

void CrystalWindow_ClipboardCopyWithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data, utf8_string_struct format), P_INSTANCE(DataInterchange)  data)
{
    DataInterchange_CreateContext(data);
	data->selection_type = DataInterchange::E_CLIPBOARD;

    IDataObject* pDataObject = (IDataObject *)data->context;

    HRESULT hr = OleSetClipboard(pDataObject);
    if (FAILED(hr)) {
        if (hr == CO_E_NOTINITIALIZED) {
            std::cerr << "Failed to set clipboard data. COM/OLE not initialized on this thread. "
                         "Ensure Application_Init was called and the thread is STA." << std::endl;
        } else {
            std::cerr << "Failed to set clipboard data. HRESULT: " << std::hex << hr << std::endl;
        }
    }

    pDataObject->Release();

    data->provide_chosen = provide;
}

void CrystalWindow_ClipboardCopyPersist(P_INSTANCE(DataInterchange) dataInterchange) {
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

void CrystalWindow_ClipboardClear()
{
    if (!OpenClipboard(nullptr)) {
        std::cerr << "Failed to open clipboard." << std::endl;
        return;
    }
    EmptyClipboard();
    CloseClipboard();
}
}