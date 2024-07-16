#include "DragDrop_X11.h"
#include <iostream>
#include <X11/Xatom.h>
#include <X11/Xlib.h>
#include <string.h>
#include <locale>
#include <codecvt>
#include <iomanip>
#include <sstream>
#include <string>

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "DragDrop_X11:"

// Function to get callbacks associated with a window handle
P_INSTANCE(WindowCallbacks)  get_callbacks(P_INSTANCE(WindowHandle) handle);

// Function to convert DragActions to X11 int64_t
int64_t drag_actions_to_xint64_t(DragActions actions, P_INSTANCE(Display)  display) {
    int64_t result = 0;

    if (actions & DRAG_OPERATION_COPY) {
        result |= XInternAtom(display, "XdndActionCopy", False);
    } else if (actions & DRAG_OPERATION_MOVE) {
        result |= XInternAtom(display, "XdndActionMove", False);
    } else if (actions & DRAG_OPERATION_LINK) {
        result |= XInternAtom(display, "XdndActionLink", False);
    }

    return result;
}

// Function to convert X11 int64_t to DragActions
DragActions xint64_t_to_drag_actions(int64_t xint64_t, P_INSTANCE(Display)  display) {
    DragActions actions = DRAG_OPERATION_NONE;

    // Translate XdndActions to DragActions
    int64_t actions_none = (xint64_t & XInternAtom(display, "XdndActionNone", False));
    int64_t actions_and_copy = (xint64_t & XInternAtom(display, "XdndActionCopy", False) ^ actions_none);
    int64_t actions_and_move = (xint64_t & XInternAtom(display, "XdndActionMove", False) ^ actions_none);
    int64_t actions_and_link = (xint64_t & XInternAtom(display, "XdndActionLink", False) ^ actions_none);

    // Initialize supported operat
    // Assign supported actions based on the action masks
    if (actions_and_copy) actions = (DragActions) (actions | DRAG_OPERATION_COPY);
    if (actions_and_move) actions = (DragActions) (actions | DRAG_OPERATION_MOVE);
    if (actions_and_link) actions = (DragActions) (actions | DRAG_OPERATION_LINK);

    return actions;
}

// Function to register the window as a drag target
void CrystalWindow_X11::RegisterDragTarget() {
    Atom XdndAware = XInternAtom(display, "XdndAware", False);

    // Set the XdndAware property on the window to indicate it supports drag-and-drop
    int64_t version = 5; // Xdnd version
    XChangeProperty(display, window, XdndAware, XA_ATOM, 32, PropModeReplace, (P_ELEMENTS(uint8_t) )&version, 1);
    std::cerr << mod_header() << "Window registered as drag target with XdndAware version " << version << std::endl;
}

// Function to send XdndStatus message
void send_xdnd_status(P_INSTANCE(Display)  display, Window target_window, Window source_window, const DragStatus status) {
    XEvent reply;
    memset(&reply, 0, sizeof(reply));
    reply.type = ClientMessage;
    reply.xclient.display = display;
    reply.xclient.window = target_window;
    reply.xclient.message_type = XInternAtom(display, "XdndStatus", False);
    reply.xclient.format = 32;
    reply.xclient.data.l[0] = source_window;
    reply.xclient.data.l[1] = status.accept ? 1 : 0; // Accept the drop
    reply.xclient.data.l[2] = 0; // No rectangle
    reply.xclient.data.l[3] = 0; // No rectangle

    reply.xclient.data.l[4] = drag_actions_to_xint64_t(status.action, display);

    /*
    if (status.actions & DRAG_OPERATION_COPY) {
        reply.xclient.data.l[4] = XInternAtom(display, "XdndActionCopy", False);
    } else if (status.actions & DRAG_OPERATION_MOVE) {
        reply.xclient.data.l[4] = XInternAtom(display, "XdndActionMove", False);
    } else if (status.actions & DRAG_OPERATION_LINK) {
        reply.xclient.data.l[4] = XInternAtom(display, "XdndActionLink", False);
    } else {
        reply.xclient.data.l[4] = XInternAtom(display, "XdndActionNone", False);
    }*/

    XSendEvent(display, target_window, False, NoEventMask, &reply);
    std::string status_message;

    if (status.accept) {
        status_message = "accept " + DragActions_AsString(status.action);
    } else status_message = "reject";

    std::cerr << mod_header() << "XdndStatus event sent to provide feedback to " << status_message << std::endl;
}

// Function to send XdndFinished message
void send_xdnd_finished(P_INSTANCE(Display)  display, Window source_window, Window target_window, bool success) {
    XEvent reply;
    memset(&reply, 0, sizeof(reply));
    reply.type = ClientMessage;
    reply.xclient.display = display;
    reply.xclient.window = target_window;
    reply.xclient.message_type = XInternAtom(display, "XdndFinished", False);
    reply.xclient.format = 32;
    reply.xclient.data.l[0] = source_window;
    reply.xclient.data.l[1] = success ? 1 : 0; // Drop completed successfully or not
    reply.xclient.data.l[2] = XInternAtom(display, "XdndActionCopy", False); // Action

    XSendEvent(display, target_window, False, NoEventMask, &reply);
    std::cerr << mod_header() << "XdndFinished message sent" << std::endl;
}

// Function to handle XdndEnter message
bool CrystalWindow_X11::handle_xdnd_enter(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(display, "XdndEnter", False)) return false;

    std::cerr << mod_header() << "XdndEnter event received" << std::endl;

    if (callbacks.on_drag_receive_enter) {
        // Enumerate drop types present in XdndEnter
        P_ELEMENTS(Atom) drop_types = nullptr;

        int32_t num_types = 0;
        bool has_more_than_3_types = event->xclient.data.l[1] & 1;

        if (has_more_than_3_types) {
            std::cerr << mod_header() << "More than 3 types in XdndEnter" << std::endl;
            Atom XdndTypeList = XInternAtom(display, "XdndTypeList", False);
            Atom actual_type;
            int32_t actual_format;
            uint64_t nitems, bytes_after;
            P_ELEMENTS(uint8_t) prop;

            if (XGetWindowProperty(display,
                event->xclient.data.l[0], XdndTypeList, 0, (~0L), False, XA_ATOM,
                &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success) {
                drop_types = (P_ELEMENTS(Atom))prop;
                num_types = (int32_t) nitems;
            }
        } else {
            drop_types = new Atom[3];
            drop_types[0] = event->xclient.data.l[2];
            drop_types[1] = event->xclient.data.l[3];
            drop_types[2] = event->xclient.data.l[4];
            num_types = 3;
            while (num_types > 0 && drop_types[num_types - 1] == None) num_types--;
        }

        // Create DragDropData and associate it with the window_handle
        current_drag_receive_data = DragDropData_Create();
        std::cerr << mod_header() << "DragDropData created" << std::endl;

        // Store drop types in DragDropData
        for (int32_t i = 0; i < num_types; ++i) {
            if (drop_types[i] != None) {
                utf8_string type_name = XGetAtomName(display, drop_types[i]);
                std::cerr << mod_header() << "Format: " << type_name << std::endl;

                if (strcmp(type_name, "text/plain") == 0) {
                    DragDropData_FormatAdd(current_drag_receive_data, "text/plain");
                } else if (strcmp(type_name, "text/html") == 0) {
                    DragDropData_FormatAdd(current_drag_receive_data, "text/html");
                } else if (strcmp(type_name, "text/uri-list") == 0) {
                    DragDropData_FormatAdd(current_drag_receive_data, "text/file-uri");
                }
                XFree(type_name);
            }
        }

        if (drop_types && has_more_than_3_types) {
            XFree(drop_types);
        } else {
            delete[] drop_types;
        }

        // Read the supported actions from the property if there are fewer than three types
        //if (!has_more_than_3_types)
            {
            Atom XdndSupportedActions = XInternAtom(display, "XdndSupportedActions", False);
            Atom actual_type;
            int32_t actual_format;
            uint64_t nitems, bytes_after;
            P_ELEMENTS(uint8_t) prop;

            if (XGetWindowProperty(display,
                event->xclient.data.l[0], XdndSupportedActions, 0, (~0L), False, XA_ATOM,
                &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success) {
                if (prop != nullptr) {
                    int64_t actions = *(P_ELEMENTS(int64_t))prop;
                    current_drag_receive_data->action_selections = xint64_t_to_drag_actions(actions, display);
                    XFree(prop);
                }
            }
        }

        callbacks.on_drag_receive_enter(myHandle, current_drag_receive_data);
        std::cerr << mod_header() << "Drag enter event" << std::endl;

        return true;
    }

    return false;
}

bool CrystalWindow_X11::handle_xdnd_position(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(display, "XdndPosition", False)) return false;

    uint64_t val = (uint64_t) event->xclient.data.l[2];

    int32_t x = ( val >> 16) & 0xFFFF;
    int32_t y = val & 0xFFFF;

    CoordsFromRoot(x, y);

    std::cerr << mod_header() << "XdndPosition event received. x="<<x<<" y="<< y
    << " l[0]=" << std::hex << std::setw(8) << std::setfill('0') << event->xclient.data.l[0] << std::dec
    << " l[1]=" << std::hex << std::setw(8) << std::setfill('0') << event->xclient.data.l[1] << std::dec
    << " l[3]=" << std::hex << std::setw(8) << std::setfill('0') << event->xclient.data.l[3] << std::dec
    << " l[4]=" << std::hex << std::setw(8) << std::setfill('0') << event->xclient.data.l[4] << std::dec
    << std::endl;

    current_drag_receive_data->status.action = xint64_t_to_drag_actions(event->xclient.data.l[4], display);

    if (callbacks.on_drag_receive_motion) {

        // Extract pointer position from l[2]

        // Extract key state from l[1]
        // Note: Currently, l[1] is used for CurrentTime, not key state. This needs further clarification.

        // Call the drag receive motion callback

        callbacks.on_drag_receive_motion(myHandle, current_drag_receive_data, x, y, 0 /* key_state is not available here */);
        current_drag_receive_data->has_status = true;

        // Send the status response
        send_xdnd_status(display, event->xclient.data.l[0], window, current_drag_receive_data->status);
        return true;
    }

    return false;
}

// Function to handle XdndLeave message
bool CrystalWindow_X11::handle_xdnd_leave(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(display, "XdndLeave", False)) return false;

    std::cerr << mod_header() << "XdndLeave event received" << std::endl;
    if (callbacks.on_drag_receive_leave) {
        callbacks.on_drag_receive_leave(myHandle, current_drag_receive_data);
        if (current_drag_receive_data) {
            DragDropData_Free(current_drag_receive_data);
            current_drag_receive_data = nullptr;
        }
        return true;
    }

    return false;
}

// Function to handle XdndDrop message
bool CrystalWindow_X11::handle_xdnd_drop(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(display, "XdndDrop", False)) return false;

    std::cerr << mod_header() << "XdndDrop event received" << std::endl;
    if (callbacks.on_drag_receive_select) {
        utf8_string_const format = nullptr;
        callbacks.on_drag_receive_select(myHandle, current_drag_receive_data, &format);

        utf8_string_const xformat = nullptr;

        if (strcmp(format, "text/plain") == 0) {
            // Store type as text/plain
            xformat = "text/plain";
        } else if (strcmp(format, "text/html") == 0) {
            // Store type as text/html
            xformat = "text/html";
        } else if (strcmp(format, "text/file-uri") == 0) {
            // Store type as text/uri-list
            xformat = "text/uri-list";
        }

        std::cerr << mod_header() << "Selected format: " << xformat << std::endl;

        Atom XdndSelection = XInternAtom(display, "XdndSelection", False);
        Atom target = XInternAtom(display, xformat, False);

        XConvertSelection(display, XdndSelection, target, XdndSelection, window, CurrentTime);
        std::cerr << mod_header() << "XConvertSelection called with target: " << xformat << std::endl;
        return true;
    }

    return false;
}

// Function to handle SelectionNotify events
bool CrystalWindow_X11::handle_selection_notify(P_INSTANCE(XEvent)  event) {
    if (event->type != SelectionNotify) return false;

    std::cerr << mod_header() << "SelectionNotify event received" << std::endl;

    if (event->xselection.property) {
        Atom actual_type;
        int32_t actual_format;
        uint64_t nitems, bytes_after;
        P_ELEMENTS(uint8_t) prop;
        if (XGetWindowProperty(event->xselection.display, event->xselection.requestor, event->xselection.property, 0, (~0L), False, AnyPropertyType, &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success) {
            utf8_string format = XGetAtomName(event->xselection.display, actual_type);
            std::cerr << mod_header() << "SelectionNotify format: " << format << std::endl;

            if (strcmp("text/html", format) == 0) {
                std::string text_data;

                if (nitems >= 2 && prop[0] == 0xFF && prop[1] == 0xFE) {
                    std::u16string utf16_data(reinterpret_cast<P_ELEMENTS(const char16_t)>(prop + 2), (nitems - 2) / 2);
                    std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
                    text_data = convert.to_bytes(utf16_data);
                } else if ((prop[0] >= ' ' && prop[2] >= ' ') && (prop[1] == 0 && prop[3] == 0)) {
                    std::u16string utf16_data(reinterpret_cast<P_ELEMENTS(const char16_t)>(prop), (nitems) / 2);
                    std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
                    text_data = convert.to_bytes(utf16_data);
                } else {
                    text_data = std::string((utf8_string )prop, nitems);
                }

                std::cerr << mod_header() << "Setting selection for text/html: " << text_data << std::endl;
                DragDropData_Selection_Set(current_drag_receive_data, format, (utf8_string )text_data.c_str(), text_data.length());
                XFree(prop);
            } else if (strcmp("text/plain", format) == 0) {
                std::cerr << mod_header() << "Setting selection for text/plain" << std::endl;
                DragDropData_Selection_Set(current_drag_receive_data, format, prop, nitems);
                XFree(prop);
            } else if (strcmp("text/uri-list", format) == 0) {
                std::string uri_list(reinterpret_cast<utf8_string >(prop), nitems);
                std::istringstream stream(uri_list);
                std::string line;
                std::string cleaned_uri_list;

                while (std::getline(stream, line)) {
                    if (line.rfind("file://", 0) == 0) {
                        line = line.substr(7); // Remove 'file://' prefix
                    }
                    cleaned_uri_list += line + "\n";
                }

                std::cerr << mod_header() << "Setting selection for text/uri-list: " << cleaned_uri_list << std::endl;
                DragDropData_Selection_Set(current_drag_receive_data, "text/file-uri", cleaned_uri_list.data(), cleaned_uri_list.size());
                XFree(prop);
            }

            XDeleteProperty(event->xselection.display, event->xselection.requestor, event->xselection.property);
        }
        if (callbacks.on_drag_receive_drop) {
            callbacks.on_drag_receive_drop(myHandle, current_drag_receive_data);
        }
        send_xdnd_finished(event->xselection.display, window, event->xselection.requestor, current_drag_receive_data->status.accept);
    } else {
        std::cerr << mod_header() << "Selection conversion failed." << std::endl;
        if (callbacks.on_drag_receive_drop) {
            callbacks.on_drag_receive_drop(myHandle, nullptr);
        }
        send_xdnd_finished(event->xselection.display, window, event->xselection.requestor, false);

        if (current_drag_receive_data) {
            DragDropData_Free(current_drag_receive_data);
            current_drag_receive_data = nullptr;
        }
    }

    return true;
}

// Function to handle ClientMessage events
bool CrystalWindow_X11::handle_client_message(P_INSTANCE(XEvent)  event) {

    if (handle_xdnd_enter(event) || handle_xdnd_position(event) || handle_xdnd_leave(event) ||handle_xdnd_drop(event)) return true;
    return false;
}

// Function to handle X events related to drag and drop
bool CrystalWindow_X11::handle_drop_xevents(P_INSTANCE(XEvent)  event) {

    if (handle_client_message(event)) return true;
    if (handle_selection_notify(event)) return true;
    return false;
}

void CrystalWindow_X11::DragStart(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
    // Store the DragDropData for later use in SelectionRequest
    if (!drag_provide) { drag_provide = new DragProvide_X11(this); }
    drag_provide->StartDrag(data, x, y);

}
