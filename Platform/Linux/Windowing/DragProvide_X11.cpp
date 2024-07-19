// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalWindow_X11.h"
#include "../Platform.h"

#include <cstring>
#include <sstream>
#include <iomanip>

#include "DragDrop_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() std::hex << std::setw(8) << std::setfill('0') << "DragProvide_X11: source=" << source_window->window << " target=" << target_window << " << " << std::dec

DragProvide_X11::DragProvide_X11(P_INSTANCE(CrystalWindow_X11)source_window)
    : source_window(source_window), target_window(None), drag_data(nullptr), dragging(false), entered(false) {
}

DragProvide_X11::~DragProvide_X11() {
    if (dragging) {
        EndDrag(false);
    }
}

void DragProvide_X11::StartDrag(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
    std::cerr << mod_header() << "CrystalWindow_DragStart" << std::endl;

    Atom XA_TARGETS = XInternAtom(source_window->display, "TARGETS", False);
    Atom XA_TEXT = XInternAtom(source_window->display, "TEXT", False);
    Atom XA_XdndSelection = XInternAtom(source_window->display, "XdndSelection", False);

    XSetSelectionOwner(source_window->display, XA_XdndSelection, source_window->window, CurrentTime);

    if (XGetSelectionOwner(source_window->display, XA_XdndSelection) != source_window->window) {
        std::cerr << mod_header() << "Failed to acquire selection" << std::endl;
        return; // Selection not acquired
    }

    std::cerr << mod_header() << "Selection acquired" << std::endl;

    drag_data = data;
    dragging = true;

    clear_target();

    source_window->MouseCapture();

    Atom XdndAware = XInternAtom(source_window->display, "XdndAware", False);
    int64_t version = 5; // Xdnd version
    XChangeProperty(source_window->display, source_window->window, XdndAware, XA_ATOM, 32, PropModeReplace, (P_ELEMENTS(uint8_t) )&version, 1);

    UpdateMotion(x, y);

    std::cerr << mod_header() << "Drag start setup complete" << std::endl;
}

void DragProvide_X11::clear_target() {
    target_window = None;
    wait_for_status = false;
    if (drag_data) {
        drag_data->has_status = false;
        drag_data->status.accept = false;
        drag_data->status.action = DRAG_OPERATION_NONE;
    }
}

void DragProvide_X11::UpdateMotion(int32_t x, int32_t y) {
    if (!dragging) return;

    /* convert x and y to root coordinates and find the window under them */
    Window new_target = get_window_under_cursor(x, y);

    if (new_target != target_window) {
        if (entered) {
            send_xdnd_leave();
            entered = false;
            clear_target();
        }
        if (new_target != None) {
            target_window = new_target;
            send_xdnd_enter();
            entered = true;
        }
    }

    if (entered) {
        if (!wait_for_status) {
            send_xdnd_position(x, y);
        }
    }
}

bool DragProvide_X11::handle_message(P_INSTANCE(XEvent) event) {
    if (!dragging) return false;

    switch (event->type) {
        case MotionNotify:
            UpdateMotion(event->xmotion.x, event->xmotion.y);
            return true;
        case ButtonRelease:
            EndDrag(true);
            return true;
    }

    if (handle_xdnd_status(event)) return true;
    if (handle_selection_request(event)) return true;
    if (handle_xdnd_finished(event)) return true;

    return false;
}

bool DragProvide_X11::handle_selection_request(P_INSTANCE(XEvent) event) {
    if (event->type != SelectionRequest) return false;

    std::cerr << mod_header() << "SelectionRequest event received" << std::endl;
    XSelectionRequestEvent *req = &event->xselectionrequest;
    XSelectionEvent ev = {0};
    ev.type = SelectionNotify;
    ev.display = req->display;
    ev.requestor = req->requestor;
    ev.selection = req->selection;
    ev.target = req->target;
    ev.time = req->time;
    ev.property = req->property;

    std::cerr << mod_header() << "SelectionRequest for target: " << XGetAtomName(req->display, req->target) << std::endl;

    if (req->target == XInternAtom(req->display, "TARGETS", False)) {
        Atom types[2] = {XInternAtom(req->display, "STRING", False), XInternAtom(req->display, "UTF8_STRING", False)};
        XChangeProperty(req->display, req->requestor, req->property, XA_ATOM, 32, PropModeReplace, (P_ELEMENTS(uint8_t) )types, 2);
    } else if (drag_data != nullptr) {
        utf8_string_const format = nullptr;

        if (req->target == XInternAtom(req->display, "text/plain", False)) format = "text/plain";
        else if (req->target == XInternAtom(req->display, "text/html", False)) format = "text/html";
        else if (req->target == XInternAtom(req->display, "text/uri-list", False)) format = "text/file-uri";

        std::cerr << mod_header() << "Chosen format: " << format << std::endl;

        source_window->callbacks.on_drag_provide_chosen(source_window->myHandle, drag_data, format);
        P_INSTANCE(void) d;
        size_t sz;
        DragDropData_Selection_Reveal(drag_data, nullptr, &d, &sz);

        std::cerr << mod_header() << "Providing data for format: " << format << std::endl;
        if (strcmp(format, "text/file-uri") == 0) {
            std::string uri_list(reinterpret_cast<utf8_string >(d), sz);
            std::istringstream stream(uri_list);
            std::string line;
            std::string cleaned_uri_list;

            while (std::getline(stream, line)) {
                std::cerr << mod_header() << "line = \"" << line << "\"" << std::endl;

                if (!line.empty()) {
                    if (line.find("://") == std::string::npos) {
                        line = "file://" + line; // Add 'file://' prefix if not present
                    }
                    cleaned_uri_list += line + "\n";
                }
            }

            std::cerr << mod_header() << "Cleaned URI list: " << cleaned_uri_list << std::endl;
            XChangeProperty(req->display, req->requestor, req->property, req->target, 8, PropModeReplace, (P_ELEMENTS(uint8_t) )cleaned_uri_list.data(), cleaned_uri_list.size());
        } else {
            XChangeProperty(req->display, req->requestor, req->property, req->target, 8, PropModeReplace, (P_ELEMENTS(uint8_t) )d, sz);
        }
    } else {
        std::cerr << mod_header() << "No current_drag_provide_data available" << std::endl;
    }
    XSendEvent(req->display, req->requestor, False, 0, (P_INSTANCE(XEvent) )&ev);

    return true;
}

bool DragProvide_X11::handle_xdnd_status(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(source_window->display, "XdndStatus", False)) return false;

    std::cerr << mod_header() << std::hex << std::setw(8) << std::setfill('0') << "status receive l[1] = " << event->xclient.data.l[1] << std::dec << std::endl;

    bool accept = event->xclient.data.l[1] & 1;
    int64_t action = event->xclient.data.l[4];

    // Read the target rectangle
    int32_t x = (event->xclient.data.l[2] >> 16) & 0xFFFF;
    int32_t y = event->xclient.data.l[2] & 0xFFFF;
    int32_t width = (event->xclient.data.l[3] >> 16) & 0xFFFF;
    int32_t height = event->xclient.data.l[3] & 0xFFFF;

    drag_data->status.accept = accept;
    drag_data->status.action = xint64_t_to_drag_actions(action, source_window->display);

    std::cerr << mod_header() << "XdndStatus received. Accept: " << accept
                  << ", Action: " << DragActions_AsString(drag_data->status.action)
                  << ", Rect: [" << x << ", " << y << ", " << width << ", " << height << "]"
                  << std::endl;

    wait_for_status = false;
    drag_data->has_status = true;

    source_window->callbacks.on_drag_provide_status(source_window->myHandle, drag_data);
    return true;
}

void DragProvide_X11::drag_is_finished(bool success) {
    std::cerr << mod_header() << "drag_is_finished(success = " << (success ? "true" : "false") << ")" << std::endl;

    source_window->callbacks.on_drag_provide_finished(source_window->myHandle, drag_data, success);

    clear_target();

    dragging = false;
    drag_data = nullptr;

    source_window->MouseRelease();
}

bool DragProvide_X11::handle_xdnd_finished(P_INSTANCE(XEvent)  event) {
    if (event->type != ClientMessage) return false;
    if (event->xclient.message_type != XInternAtom(source_window->display, "XdndFinished", False)) return false;

    bool success = event->xclient.data.l[2] != 0;

    std::cerr << mod_header() << "XdndFinished received" << std::endl;

    drag_is_finished(success);

    return true;
}

void DragProvide_X11::EndDrag(bool success) {
    if (!dragging) return;

    bool fin = true;

    if (entered) {
        if (success) {
            if (drag_data->has_status) { send_xdnd_drop(); fin = false; }
        } else {
            send_xdnd_leave();
        }
    }

    if (fin) {
        drag_is_finished(false);
    }

}

Window window_at_coordinates(P_INSTANCE(Display) display, Window parent, int32_t x, int32_t y) {
    Window root_return, child_return;
    Window target = None;
    int32_t root_x, root_y, win_x, win_y;
    uint32_t mask_return;

    if (!XTranslateCoordinates(display, parent, parent, x, y, &root_x, &root_y, &child_return)) {
        return None;
    }

    // Loop to find the deepest child window at the coordinates
    while (true) {
        if (!XQueryPointer(display, child_return, &root_return, &child_return, &root_x, &root_y, &win_x, &win_y, &mask_return)) {
            break;
        }

        if (child_return == None) {
            break;
        }

        target = child_return;
    }


    return target;
}

/* convert x and y to root coordinates and find the window under them */
Window DragProvide_X11::get_window_under_cursor(int32_t &x, int32_t &y) {
    Window root = DefaultRootWindow(source_window->display);
    Window child_return, root_return;

    source_window->CoordsToRoot(x, y);

    Window target_window = window_at_coordinates(source_window->display, root, x, y);

    return target_window;
}

void DragProvide_X11::send_xdnd_enter() {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.display = source_window->display;
    event.xclient.window = target_window;
    event.xclient.message_type = XInternAtom(source_window->display, "XdndEnter", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;

    std::cerr << mod_header() << "sending XdndEnter message sent to " << target_window << " from " << source_window->window << std::endl;

    int32_t type_count = 0;
    for (P_INSTANCE(DragDropData::Node) node = DragDropData_FormatEnum(drag_data); node != nullptr; node = DragDropData_FormatEnum_Next(node)) {
        type_count++;
    }

    Atom *drop_types = new Atom[type_count];
    int32_t I = 0;

    for (P_INSTANCE(DragDropData::Node) node = DragDropData_FormatEnum(drag_data); node != nullptr; node = DragDropData_FormatEnum_Next(node)) {
        utf8_string_const ty;
        DragDropData_FormatEnum_Text(node, &ty);

        if (strcmp(ty, "text/plain") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/plain", False);
        } else if (strcmp(ty, "text/html") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/html", False);
        } else if (strcmp(ty, "text/file-uri") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/uri-list", False);
        } else {
            std::cerr << mod_header() << "DragProvide_X11::send_xdnd_enter can't convert " << ty << " to an Atom" << std::endl;
            delete[] drop_types; // Free allocated memory before throwing
            throw std::runtime_error("Unsupported format type");
        }
    }

    // Set the version in the most significant byte and indicate if more than three types are used
    event.xclient.data.l[1] = (5 << 24); // Set version 5
    if (type_count > 3) {
        event.xclient.data.l[1] |= 1; // Set the "more than three types" bit
    }

    if (type_count > 3) {
        Atom XdndTypeList = XInternAtom(source_window->display, "XdndTypeList", False);
        XChangeProperty(
            source_window->display,
            source_window->window,
            XdndTypeList,
            XA_ATOM,
            32,
            PropModeReplace,
            reinterpret_cast<P_ELEMENTS(uint8_t)>(drop_types),
            type_count
        );
    } else {
        for (int32_t i = 0; i < type_count; ++i) {
            event.xclient.data.l[2 + i] = drop_types[i];
        }
    }
    // Use a separate property to store the supported actions
    Atom XdndSupportedActions = XInternAtom(source_window->display, "XdndSupportedActions", False);

    int64_t actions = drag_actions_to_xint64_t(drag_data->action_selections, source_window->display);

    std::cerr << mod_header() << DragActions_AsString(drag_data->action_selections) << " translated as "
        << std::hex << std::setw(8) << std::setfill('0') << actions << std::dec << std::endl;

    XChangeProperty(
        source_window->display,
        source_window->window,
        XdndSupportedActions,
        XA_ATOM,
        32,
        PropModeReplace,
        reinterpret_cast<P_ELEMENTS(uint8_t)>(&actions),
        1
    );

    delete[] drop_types;
    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndEnter message sent to " << target_window << " from " << source_window->window << std::endl;
}

/*
void DragProvide_X11::send_xdnd_enter() {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.display = source_window->display;
    event.xclient.window = target_window;
    event.xclient.message_type = XInternAtom(source_window->display, "XdndEnter", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;
    event.xclient.data.l[1] = 0; // Initialize to 0

    int32_t type_count = 0;
    for (P_INSTANCE(DragDropData::Node) node = DragDropData_FormatEnum(drag_data); node != nullptr; node = DragDropData_FormatEnum_Next(node)) {
        type_count++;
    }

    Atom *drop_types = new Atom[type_count];
    int32_t I = 0;

    for (P_INSTANCE(DragDropData::Node) node = DragDropData_FormatEnum(drag_data); node != nullptr; node = DragDropData_FormatEnum_Next(node)) {
        utf8_string_const ty;
        DragDropData_FormatEnum_Text(node, &ty);

        if (strcmp(ty, "text/plain") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/plain", False);
        } else if (strcmp(ty, "text/html") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/html", False);
        } else if (strcmp(ty, "text/file-uri") == 0) {
            drop_types[I++] = XInternAtom(source_window->display, "text/uri-list", False);
        } else {
            std::cerr << mod_header() << "DragProvide_X11::send_xdnd_enter can't convert " << ty << " to an Atom" << std::endl;
            delete[] drop_types; // Free allocated memory before throwing
            throw std::runtime_error("Unsupported format type");
        }
    }

    if (type_count > 3) {
        event.xclient.data.l[1] = 1; // More than 3 types

        Atom XdndTypeList = XInternAtom(source_window->display, "XdndTypeList", False);
        XChangeProperty(
            source_window->display,
            source_window->window,
            XdndTypeList,
            XA_ATOM,
            32,
            PropModeReplace,
            reinterpret_cast<P_ELEMENTS(uint8_t)>(drop_types),
            type_count
        );
    } else {
        event.xclient.data.l[1] = 0; // 3 or fewer types
        for (int32_t i = 0; i < type_count; ++i) {
            event.xclient.data.l[2 + i] = drop_types[i];
        }

        // Use a separate property to store the supported actions
        Atom XdndSupportedActions = XInternAtom(source_window->display, "XdndSupportedActions", False);
        int64_t actions = 0;

        if (drag_data->action_selections & DRAG_OPERATION_MOVE) {
            actions |= XInternAtom(source_window->display, "XdndActionCopy", False);
        }
        if (drag_data->action_selections & DRAG_OPERATION_MOVE) {
            actions |= XInternAtom(source_window->display, "XdndActionMove", False);
        }
        if (drag_data->action_selections & DRAG_OPERATION_LINK) {
            actions |= XInternAtom(source_window->display, "XdndActionLink", False);
        }
        XChangeProperty(
            source_window->display,
            source_window->window,
            XdndSupportedActions,
            XA_ATOM,
            32,
            PropModeReplace,
            reinterpret_cast<P_ELEMENTS(uint8_t)>(&actions),
            1
        );
    }

    delete[] drop_types;
    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndEnter message sent to " << target_window << " from " << source_window->window << std::endl;
}*/

void DragProvide_X11::send_xdnd_position(int32_t x, int32_t y) {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.display = source_window->display;
    event.xclient.window = target_window;

    event.xclient.message_type = XInternAtom(source_window->display, "XdndPosition", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;
    event.xclient.data.l[1] = 0; // Use CurrentTime as suggested
    event.xclient.data.l[2] = (x << 16) | y; // Pack the coordinates into l[2]
    event.xclient.data.l[3] = CurrentTime;
    event.xclient.data.l[4] = drag_actions_to_xint64_t(drag_data->action_selections, source_window->display);

    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndPosition message sent to " << target_window << " x=" << x << " y=" << y
    << " l[0]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[0] << std::dec
    << " l[1]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[1] << std::dec
    << " l[3]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[3] << std::dec
    << " l[4]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[4] << std::dec
    << std::endl;

    drag_data->has_status = false;
    wait_for_status = true;
}

void DragProvide_X11::send_xdnd_leave() {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.window = target_window;
    event.xclient.message_type = XInternAtom(source_window->display, "XdndLeave", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;

    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndLeave message sent to " << target_window << std::endl;
}

void DragProvide_X11::send_xdnd_drop() {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.display = source_window->display;
    event.xclient.window = target_window;
    event.xclient.message_type = XInternAtom(source_window->display, "XdndDrop", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;
    event.xclient.data.l[1] = CurrentTime;

    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndDrop message sent to " << target_window << std::endl;
}

void DragProvide_X11::send_xdnd_finished(bool success) {
    XEvent event;
    memset(&event, 0, sizeof(event));
    event.type = ClientMessage;
    event.xclient.display = source_window->display;
    event.xclient.window = target_window;
    event.xclient.message_type = XInternAtom(source_window->display, "XdndFinished", False);
    event.xclient.format = 32;
    event.xclient.data.l[0] = source_window->window;
    event.xclient.data.l[1] = success ? 1 : 0; // Success flag
    event.xclient.data.l[2] = XInternAtom(source_window->display, "XdndActionCopy", False);

    XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
    XFlush(source_window->display);

    std::cerr << mod_header() << "XdndFinished message sent to " << target_window << std::endl;
}
