#include "Clipboard_X11.h"

#include <iostream>
#include <X11/Xatom.h>
#include <X11/Xlib.h>
#include <string.h>
#include <locale>
#include <codecvt>
#include <iomanip>
#include <sstream>
#include <string>

#include "CrystalWindow_X11.h"
#include "../CrystalApplication_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "Clipboard_X11:"

using namespace JWCEssentials;

namespace NewAge {
    //#include "SimpleDataObject.h"

    struct DataContext {
        CrystalWindow_X11 *window;
    };

    void DataImterchange_FormatsFromAtomArray(P_INSTANCE(DataInterchange) dataInterchange, P_ELEMENTS(Atom) types, int num_types) {
        Display *display = ((CrystalWindow_X11 *) dataInterchange->m_handle->crystal_window)->display;

        for (int32_t i = 0; i < num_types; ++i) {
            if (types[i] != None) {
                utf8_string_struct type_name = XGetAtomName_struct(display, types[i]);
                std::cerr << mod_header() << "Format: " << type_name << std::endl;

                if (strcmp(type_name, "text/plain") == 0) {
                    DataInterchange_FormatAdd(dataInterchange, "text/plain");
                } else if (strcmp(type_name, "text/html") == 0) {
                    DataInterchange_FormatAdd(dataInterchange, "text/html");
                } else if (strcmp(type_name, "text/uri-list") == 0) {
                    DataInterchange_FormatAdd(dataInterchange, "text/file-uri");
                }
            }
        }
    }

    bool FormatToAtom(Display * display, utf8_string_struct format, P_OUT(Atom) atom) {
        *atom = 0;

        if (strcmp(format, "text/plain") == 0) {
            *atom= XInternAtom(display, "text/plain", False);
            return true;
        } else if (strcmp(format, "text/html") == 0) {
            *atom = XInternAtom(display, "text/html", False);
            return true;
        } else if (strcmp(format, "text/file-uri") == 0) {
            *atom = XInternAtom(display, "text/uri-list", False);
            return true;
        }

        return false;
    }

    void DataImterchange_AtomArrayFromFormats(P_INSTANCE(DataInterchange) dataInterchange, P_INSTANCE(P_ELEMENTS(Atom)) types, P_INSTANCE(int) num_types) {
        Display *display = ((CrystalWindow_X11 *) dataInterchange->m_handle->crystal_window)->display;

        int32_t type_count = 0;
        for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(dataInterchange); node != nullptr; node = DataInterchange_FormatEnum_Next(node)) {
            type_count++;
        }

        (*types) = new Atom[type_count];
        int32_t I = 0;

        for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(dataInterchange); node != nullptr; node = DataInterchange_FormatEnum_Next(node)) {
            utf8_string_struct ty;
            DataInterchange_FormatEnum_Text(node, &ty);
            if (!FormatToAtom(display, ty, (*types)+(I++))) {
                std::cerr << mod_header() << "DragProvide_X11::send_xdnd_enter can't convert " << ty << " to an Atom" << std::endl;

                throw std::runtime_error("Unsupported format type");
            }
        }

        *num_types = I;
    }





    // clipboard = AppX11->atoms.clipboard;
    // targets = AppX11->atoms.targets;
    // utf8_string = XInternAtom(display, "UTF8_STRING", False);

    P_INSTANCE(DataInterchange)  Clipboard_Paste(P_INSTANCE(WindowHandle) handle)
    {
        /*
        IDataObject* pDataObject = nullptr;
        HRESULT hr = OleGetClipboard(&pDataObject);
        if (FAILED(hr)) {
            std::cerr << "Failed to get clipboard data. HRESULT: " << std::hex << hr << std::endl;
        }
        */

        P_INSTANCE(DataInterchange) data = DataInterchange_Create();
        data->m_handle = handle;
        data->provide_chosen = DataInterchange::provide_for_clipboard;
        data->selection_type = DataInterchange::E_CLIPBOARD;

        CrystalWindow_X11 *xwin = ((CrystalWindow_X11 *)handle->crystal_window);

        xwin->current_clipboard_receive_data = data;

        Atom clipboard =AppX11->atoms.clipboard;
        Atom targets = AppX11->atoms.targets;

        XConvertSelection(xwin->display, clipboard, targets, clipboard, xwin->window, CurrentTime);

        XEvent event;

        do {
            XNextEvent(xwin->display, &event);
            ((CrystalApplication_X11 *) TheApplication)->DispatchEvent(event);
        } while (event.type != SelectionNotify);


        /*
            Atom actual_type;
            int actual_format;
            unsigned long nitems;
            unsigned long bytes_after;
            unsigned char* prop;

            Window SelectionOwner;
            SelectionOwner = XGetSelectionOwner(xwin->display, AppX11->atoms.clipboard);

            XGetWindowProperty(xwin->display, SelectionOwner, AppX11->atoms.targets, 0, ~0, False, XA_ATOM,
                               &actual_type, &actual_format, &nitems, &bytes_after, &prop);
        // XGetWindowProperty(xwin->display, xwin->window, AppX11->atoms.clipboard, 0, ~0, False, AppX11->atoms.targets,
        //                        &actual_type, &actual_format, &nitems, &bytes_after, &prop);

            if (actual_type == XA_ATOM) {
                Atom* atoms = reinterpret_cast<Atom*>(prop);
                DataImterchange_FormatsFromAtomArray(data, atoms, nitems);
            }
            XFree(prop);
        */
        return data;
    }

    void Clipboard_Copy(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data)
    {
        data->m_handle = handle;
        CrystalWindow_X11 *xwin = ((CrystalWindow_X11 *)handle->crystal_window);
        data->selection_type = DataInterchange::E_CLIPBOARD;

        xwin->current_clipboard_provide_data = data;

        XSetSelectionOwner(xwin->display, AppX11->atoms.clipboard, xwin->window, CurrentTime);
        if (XGetSelectionOwner(xwin->display, AppX11->atoms.clipboard) != xwin->window) {
            std::cerr << "Failed to set clipboard owner." << std::endl;
        }
        Atom *types;
        int num_types;

        DataImterchange_AtomArrayFromFormats(data, &types,&num_types);

        XChangeProperty(xwin->display, xwin->window, AppX11->atoms.targets, XA_ATOM, 32, PropModeReplace,
                            reinterpret_cast<const unsigned char*>(types), num_types);


        //delete types;

        data->provide_chosen = DataInterchange::provide_for_clipboard;
    }

    void Clipboard_Copy_WithCallback(void (*provide)(P_INSTANCE(DataInterchange)  data), P_INSTANCE(DataInterchange) data)
    {
        /*
        DataInterchange_CreateContext(data);

        IDataObject* pDataObject = (IDataObject *)data->context;

        HRESULT hr = OleSetClipboard(pDataObject);
        if (FAILED(hr)) {
            std::cerr << "Failed to set clipboard data. HRESULT: " << std::hex << hr << std::endl;
        }

        pDataObject->Release();

        data->provide_chosen = provide;
        */
    }

    void Clipboard_Copy_Persist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data)
    {
        CrystalWindow_X11 *xwin = ((CrystalWindow_X11 *)handle->crystal_window);

        std::cerr << mod_header() << "Clipboard_Copy_Persist()"  << std::endl;

        for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(data); node != nullptr; node = DataInterchange_FormatEnum_Next(node)) {
            utf8_string_struct ty;
            DataInterchange_FormatEnum_Text(node, &ty);

            Atom A;

            if (!FormatToAtom(xwin->display, ty, &A)) {
                std::cerr << mod_header() << "DragProvide_X11::send_xdnd_enter can't convert " << ty << " to an Atom" << std::endl;

                throw std::runtime_error("Unsupported format type");
            }

            data->provide_chosen(data, ty);

            void *data_ptr;
            size_t size;

            DataInterchange_Selection_Reveal(data, nullptr, &data_ptr, &size);

            XChangeProperty(xwin->display, xwin->window, AppX11->atoms.clipboard, A, 8, PropModeReplace,
                            reinterpret_cast<const unsigned char*>(data_ptr), size);
        }
    }

    void Clipboard_Clear()
    {
        Display *display = ((CrystalApplication_X11 *)TheApplication)->globalDisplay;

        Atom clipboard = AppX11->atoms.clipboard;
        Atom targets = AppX11->atoms.targets;

        Window selection_owner = XGetSelectionOwner(display, clipboard);
        if (selection_owner == None) {
            std::cerr << "No selection owner for clipboard to clear" << std::endl;
            return;
        }

        // Retrieve the list of available targets
        Atom actual_type;
        int actual_format;
        unsigned long nitems;
        unsigned long bytes_after;
        unsigned char* prop;

        XGetWindowProperty(display, selection_owner, targets, 0, ~0, False, XA_ATOM,
                           &actual_type, &actual_format, &nitems, &bytes_after, &prop);

        if (actual_type == XA_ATOM) {
            Atom* atoms = reinterpret_cast<Atom*>(prop);
            for (unsigned long i = 0; i < nitems; ++i) {
                XDeleteProperty(display, selection_owner, atoms[i]);
            }
        }
        XFree(prop);

        XDeleteProperty(display, selection_owner, targets);
        // Clear the selection owner
        XSetSelectionOwner(display, clipboard, None, CurrentTime);

        std::cerr << "Clipboard cleared" << std::endl;
        /*
        if (!OpenClipboard(nullptr)) {
            std::cerr << "Failed to open clipboard." << std::endl;
            return;
        }
        EmptyClipboard();
        CloseClipboard();
        */
    }

    void DataInterchange_Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format)
    {
        CrystalWindow_X11 *xwin = ((CrystalWindow_X11 *)data->m_handle->crystal_window);

        if (data->selection_type != DataInterchange::ESELECTION::E_CLIPBOARD) {
            std::cerr << "ERROR DataInterchange_Select called during drag. This endpoint should be handled internally by dnd" << std::endl;
        }


        Atom T;
        Atom ID =AppX11->atoms.clipboard;

        FormatToAtom(xwin->display,format,&T);
        XConvertSelection(xwin->display, ID, T, ID, xwin->window, CurrentTime);

        /*

            FORMATETC fmt = { 0, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };

            std::string f = format;
            if (f == "text/plain") fmt.cfFormat = CF_UNICODETEXT;
            else if (f == "text/html") fmt.cfFormat = RegisterClipboardFormat(CFSTR_HTML);
            else if (f == "text/file-uri") fmt.cfFormat = CF_HDROP;

            STGMEDIUM stg;
            HRESULT hr = ((IDataObject *)data->context)->GetData(&fmt, &stg);

            if (SUCCEEDED(hr)) {
                if (f == "text/plain" || f == "text/html") {
                    LPWSTR lpszText = static_cast<LPWSTR>(GlobalLock(stg.hGlobal));
                    if (lpszText != nullptr) {
                        std::wstring ws(lpszText);
                        std::string text_data(ws.begin(), ws.end());
                        GlobalUnlock(stg.hGlobal);

                        DataInterchange_Selection_Set(data, format, (P_INSTANCE(void))text_data.c_str(), text_data.length() + 1);
                    }
                } else if (f == "text/file-uri") {
                    HDROP hDrop = static_cast<HDROP>(GlobalLock(stg.hGlobal));
                    if (hDrop != nullptr) {
                        uint32_t  fileCount = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);


                        std::cout << "drop of " << fileCount << " files" << std::endl;

                        std::string uri = "";
                        for (uint32_t  i = 0; i < fileCount; i++) {
                            WCHAR filePath[MAX_PATH];
                            if (DragQueryFileW(hDrop, i, filePath, MAX_PATH)) {
                                std::wstring ws(filePath);
                                std::string filePathStr(ws.begin(), ws.end());
                                std::cout << "File Path: " << filePathStr << std::endl; // Debug log
                                uri += filePathStr + "\n";
                            }
                        }
                        GlobalUnlock(stg.hGlobal);

                        DataInterchange_Selection_Set(data, "text/file-uri", (P_INSTANCE(void) ) uri.c_str(),
                                                      uri.length() + 1);
                    }
                }
                ReleaseStgMedium(&stg);

            }*/
    }
}