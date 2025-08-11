// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
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

#include "../CrystalApplication_X11.h"
#include "DragProvide_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "DragDrop_X11:"

using namespace JWCEssentials;

namespace NewAge {
    // Function to get callbacks associated with a window handle
    P_INSTANCE(WindowCallbacks)  get_callbacks(P_INSTANCE(WindowHandle) handle);

    // CrystalWindow_X11.cpp (or a private X11 translation unit)

    struct X11IncrState {
        bool active = false;

        // Who/where the owner will stream chunks
        Window requestor = 0;
        Atom   property  = None;

        // First non-empty chunk’s type decides final format (e.g., UTF8_STRING, text/html)
        Atom   first_chunk_type = None;

        // Optional size hint from initial INCR property (uint32)
        uint32_t size_hint = 0;

        // Channel separation (handy when same class is used for both)
        bool is_clipboard = false;

        // Accumulator
        std::vector<uint8_t> buffer;

        // (Optional) safety
        Time started = 0;
    };

    static void DI_X11_IncrFree(void* p) {
        delete static_cast<X11IncrState*>(p);
    }

    // Helper: ensure DI has an X11IncrState
    static X11IncrState* DI_X11_get_incr(NewAge::DataInterchange* di) {
        if (!di->os_specific) {
            di->os_specific = new X11IncrState();
            di->os_specific_free = &DI_X11_IncrFree;
        }
        return static_cast<X11IncrState*>(di->os_specific);
    }


    // Function to convert DragActions to X11 int64_t
    int64_t drag_actions_to_xint64_t(DragActions actions, P_INSTANCE(Display)  display) {
        int64_t result = 0;

        if (actions & DRAG_OPERATION_COPY) {
            result |= AppX11->atoms.xdnd.action.copy;
        } else if (actions & DRAG_OPERATION_MOVE) {
            result |= AppX11->atoms.xdnd.action.move;
        } else if (actions & DRAG_OPERATION_LINK) {
            result |= AppX11->atoms.xdnd.action.link;
        }

        return result;
    }

    // Function to convert X11 int64_t to DragActions
    DragActions xint64_t_to_drag_actions(int64_t xint64_t, P_INSTANCE(Display)  display) {
        DragActions actions = DRAG_OPERATION_NONE;

        // Translate XdndActions to DragActions
        int64_t actions_none = (xint64_t & AppX11->atoms.xdnd.action.none);
        int64_t actions_and_copy = (xint64_t & AppX11->atoms.xdnd.action.copy ^ actions_none);
        int64_t actions_and_move = (xint64_t & AppX11->atoms.xdnd.action.move ^ actions_none);
        int64_t actions_and_link = (xint64_t & AppX11->atoms.xdnd.action.link ^ actions_none);

        // Initialize supported operat
        // Assign supported actions based on the action masks
        if (actions_and_copy) actions = (DragActions) (actions | DRAG_OPERATION_COPY);
        if (actions_and_move) actions = (DragActions) (actions | DRAG_OPERATION_MOVE);
        if (actions_and_link) actions = (DragActions) (actions | DRAG_OPERATION_LINK);

        return actions;
    }

    // Function to register the window as a drag target
    void CrystalWindow_X11::RegisterDragTarget() {
        Atom XdndAware = AppX11->atoms.xdnd.aware;

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
        reply.xclient.message_type = AppX11->atoms.xdnd.msg.status;
        reply.xclient.format = 32;
        reply.xclient.data.l[0] = source_window;
        reply.xclient.data.l[1] = status.accept ? 1 : 0; // Accept the drop
        reply.xclient.data.l[2] = 0; // No rectangle
        reply.xclient.data.l[3] = 0; // No rectangle

        reply.xclient.data.l[4] = drag_actions_to_xint64_t(status.action, display);

        /*
        if (status.actions & DRAG_OPERATION_COPY) {
            reply.xclient.data.l[4] = AppX11->atoms.xdnd.action.copy;
        } else if (status.actions & DRAG_OPERATION_MOVE) {
            reply.xclient.data.l[4] = AppX11->atoms.xdnd.action.move;
        } else if (status.actions & DRAG_OPERATION_LINK) {
            reply.xclient.data.l[4] = AppX11->atoms.xdnd.action.link;
        } else {
            reply.xclient.data.l[4] = AppX11->atoms.xdnd.action.none;
        }*/

        XSendEvent(display, target_window, False, NoEventMask, &reply);
        std::string status_message;

        if (status.accept) {
            status_message = "accept " + DragDropData_DragActionsString(status.action);
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
        reply.xclient.message_type = AppX11->atoms.xdnd.msg.finished;
        reply.xclient.format = 32;
        reply.xclient.data.l[0] = source_window;
        reply.xclient.data.l[1] = success ? 1 : 0; // Drop completed successfully or not
        reply.xclient.data.l[2] = AppX11->atoms.xdnd.action.copy; // Action

        XSendEvent(display, target_window, False, NoEventMask, &reply);
        std::cerr << mod_header() << "XdndFinished message sent" << std::endl;
    }

    // Function to handle XdndEnter message
    bool CrystalWindow_X11::handle_xdnd_enter(P_INSTANCE(XEvent)  event) {
        if (event->type != ClientMessage) return false;
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.enter) return false;

        std::cerr << mod_header() << "XdndEnter event received" << std::endl;

        if (callbacks.on_drag_receive_enter) {
            // Enumerate drop types present in XdndEnter
            P_ELEMENTS(Atom) drop_types = nullptr;

            int32_t num_types = 0;
            bool has_more_than_3_types = event->xclient.data.l[1] & 1;

            if (has_more_than_3_types) {
                std::cerr << mod_header() << "More than 3 types in XdndEnter" << std::endl;
                Atom XdndTypeList = AppX11->atoms.xdnd.type_list;
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
            current_drag_receive_data->selection_type = DataInterchange::E_DND;

            std::cerr << mod_header() << "DragDropData created" << std::endl;

            // Store drop types in DragDropData
            for (int32_t i = 0; i < num_types; ++i) {
                if (drop_types[i] != None) {
                    utf8_string_struct type_name = XGetAtomName_struct(display, drop_types[i]);
                    std::cerr << mod_header() << "Format: " << type_name << std::endl;

                    if (strcmp(type_name, "text/plain") == 0) {
                        DataInterchange_FormatAdd(current_drag_receive_data, "text/plain");
                    } else if (strcmp(type_name, "text/html") == 0) {
                        DataInterchange_FormatAdd(current_drag_receive_data, "text/html");
                    } else if (strcmp(type_name, "text/uri-list") == 0) {
                        DataInterchange_FormatAdd(current_drag_receive_data, "text/file-uri");
                    }
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
                Atom XdndSupportedActions = AppX11->atoms.xdnd.supported_actions;
                Atom actual_type;
                int32_t actual_format;
                uint64_t nitems, bytes_after;
                P_ELEMENTS(uint8_t) prop;

                if (XGetWindowProperty(display,
                    event->xclient.data.l[0], XdndSupportedActions, 0, (~0L), False, XA_ATOM,
                    &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success)
                    {
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
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.position) return false;

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
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.leave) return false;

        std::cerr << mod_header() << "XdndLeave event received" << std::endl;
        if (callbacks.on_drag_receive_leave) {
            callbacks.on_drag_receive_leave(myHandle, current_drag_receive_data);
            if (current_drag_receive_data) {
                DataInterchange_Free(current_drag_receive_data);
                current_drag_receive_data = nullptr;
            }
            return true;
        }

        return false;
    }

    // Function to handle XdndDrop message
    bool CrystalWindow_X11::handle_xdnd_drop(P_INSTANCE(XEvent)  event) {
        if (event->type != ClientMessage) return false;
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.drop) return false;

        std::cerr << mod_header() << "XdndDrop event received" << std::endl;
        if (callbacks.on_drag_receive_select) {
            utf8_string_struct format = nullptr;
            format = callbacks.on_drag_receive_select(myHandle, current_drag_receive_data);

            utf8_string_struct xformat = nullptr;

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

            Atom XdndSelection = AppX11->atoms.xdnd.selection;
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

            bool isClipboard = false;

            DataInterchange * current_data;

            if (XGetWindowProperty(event->xselection.display, event->xselection.requestor, event->xselection.property, 0, (~0L), False, AnyPropertyType,
                &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success) {
                if (event->xselection.selection == AppX11->atoms.clipboard) {
                    std::cerr << "Handling clipboard selection" << std::endl;
                    // Handle clipboard-specific logic if necessary

                    isClipboard = true;

                    current_data = this->current_clipboard_receive_data;

                } else if (event->xselection.selection == AppX11->atoms.xdnd.selection) {
                    std::cerr << "Handling drag-and-drop selection" << std::endl;
                    // Handle drag-and-drop-specific logic if necessary
                    isClipboard = false;
                    current_data = this->current_drag_receive_data;

                } else {
                    {
                        std::cerr << "UNKNOWN selection" << std::endl;
                    }
                }

                Atom INCR = XInternAtom(event->xselection.display, "INCR", False);

                if (actual_type == INCR) {
                    // 1) Read the size hint (optional)
                    //    actual_format should be 32, nitems == 1
                    //    uint32_t size_hint = prop ? *(uint32_t*)prop : 0;

                    uint32_t hint = 0;
                    if (prop && actual_format == 32 && nitems == 1) {
                        hint = *(uint32_t*)prop;
                    }
                    if (prop) XFree(prop);

                    // 2) Mark that we’re in an incremental transfer
                    // Set up state on the correct DI object
                    DataInterchange* di = current_data;
                    auto* incr = DI_X11_get_incr(di);
                    incr->active = true;
                    incr->requestor = event->xselection.requestor;
                    incr->property  = event->xselection.property;
                    incr->first_chunk_type = None;
                    incr->size_hint = hint;
                    incr->is_clipboard = isClipboard;
                    incr->buffer.clear();
                    incr->started = CurrentTime; // or X server time if you prefer


                    // 3) We must select for PropertyChange events on the requestor window
                    XSelectInput(event->xselection.display, event->xselection.requestor,
                                 PropertyChangeMask | /* keep existing masks */ StructureNotifyMask);

                    // 4) Delete the property to signal "ready for first chunk"
                    XDeleteProperty(event->xselection.display,
                                    event->xselection.requestor,
                                    event->xselection.property);

                    // Don’t call callbacks yet; we’ll finish in PropertyNotify
                    return true;
                }


                /*
                }

            if (XGetWindowProperty(event->xselection.display, event->xselection.requestor, event->xselection.property, 0, (~0L), False, AnyPropertyType, &actual_type, &actual_format, &nitems, &bytes_after, &prop) == Success) {
    */
                utf8_string_struct format = XGetAtomName_struct(event->xselection.display, actual_type);
                std::cerr << mod_header() << "SelectionNotify format: " << format << std::endl;

                current_data->selected_format = format;

                if (strcmp("ATOM", format) == 0) {
                    Atom* atoms = reinterpret_cast<Atom*>(prop);
                    DataImterchange_FormatsFromAtomArray(current_data, atoms, nitems);
                    XFree(prop);
                    return true;

                } else if (strcmp("text/html", format) == 0) {
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
                        text_data = std::string((char *)prop, nitems);
                    }

                    std::cerr << mod_header() << "Setting selection for text/html" << std::endl;

                    DataInterchange_SelectionSet(current_data, format, (void *) text_data.c_str(), text_data.length());
                    XFree(prop);
                } else if (strcmp("text/plain", format) == 0) {
                    std::cerr << mod_header() << "Setting selection for text/plain" << std::endl;
                    DataInterchange_SelectionSet(current_data, format, prop, nitems);
                    XFree(prop);
                } else if (strcmp("text/uri-list", format) == 0) {
                    utf8_string_struct uri_list;
                    uri_list.Alloc(nitems);

                    for (int i=0; i<nitems && ((char *) prop)[i]; i++)
                        uri_list.c_str[i] = ((char *) prop)[i];


                    //std::string uri_list(std::string((char *) prop), nitems);
                    std::istringstream stream(uri_list.c_str);
                    std::string line;
                    std::string cleaned_uri_list;

                    while (std::getline(stream, line)) {
                        if (line.rfind("file://", 0) == 0) {
                            line = line.substr(7); // Remove 'file://' prefix
                        }
                        cleaned_uri_list += line + "\n";
                    }

                    std::cerr << mod_header() << "Setting selection for text/uri-list: " << cleaned_uri_list << std::endl;
                    DataInterchange_SelectionSet(current_data, "text/file-uri", cleaned_uri_list.data(), cleaned_uri_list.size());
                    XFree(prop);
                }

                //XDeleteProperty(event->xselection.display, event->xselection.requestor, event->xselection.property);
                }

            if (!isClipboard) {
                if (callbacks.on_drag_receive_drop) {
                    callbacks.on_drag_receive_drop(myHandle, (DragDropData *) current_data);
                }
                send_xdnd_finished(event->xselection.display, window, event->xselection.requestor,
                    ((DragDropData *) current_data)->status.accept);
            } else {
                if (callbacks.on_clipboard_receive_data) {
                    callbacks.on_clipboard_receive_data(myHandle, current_data);
                }
            }

        } else {
            std::cerr << mod_header() << "Selection conversion failed." << std::endl;
            if (callbacks.on_drag_receive_drop) {
                callbacks.on_drag_receive_drop(myHandle, nullptr);
            }
            send_xdnd_finished(event->xselection.display, window, event->xselection.requestor, false);

            if (current_drag_receive_data) {
                DataInterchange_Free(current_drag_receive_data);
                current_drag_receive_data = nullptr;
            }

            return true;
        }

        return false;
    }

    bool CrystalWindow_X11::handle_property_notify(P_INSTANCE(XEvent)  event) {

        if (event->type != PropertyNotify) return false;

        std::cerr << mod_header() << "PropertyNotify event received" << std::endl;

        bool isClipboard = false;

        DataInterchange * current_data;

        if (event->xselection.selection == AppX11->atoms.clipboard) {
            isClipboard = true;

            current_data = this->current_clipboard_receive_data;

        } else if (event->xselection.selection == AppX11->atoms.xdnd.selection) {
            isClipboard = false;
            current_data = this->current_drag_receive_data;

        } else {
            {
                std::cerr << "UNKNOWN selection" << std::endl;
            }
        }

        auto* incr = static_cast<X11IncrState*>(current_data->os_specific);
    

        if (incr->active) {
            if (event->xproperty.window == incr->requestor &&
                event->xproperty.atom   == incr->property &&
                event->xproperty.state  == PropertyNewValue) {

                Atom type;
                int format;
                unsigned long nitems, bytes_after;
                unsigned char *data = nullptr;

                // Get and DELETE the property to acknowledge receipt of this chunk
                if (XGetWindowProperty(display, incr->requestor, incr->property,
                                       0, (~0L), True, AnyPropertyType,
                                       &type, &format, &nitems, &bytes_after, &data) == Success) {

                    if (nitems == 0) {
                        // Zero-length property => end of INCR stream
                        // Cleanup + finalize
                        incr->active = false;

                        // Decide the final format (type we saw on the first non-empty chunk)
                        if (incr->first_chunk_type != None) {
                            utf8_string_struct final_fmt = XGetAtomName_struct(display, incr->first_chunk_type);
                            current_data->selected_format = final_fmt;

                            // Dispatch to your usual handlers using the accumulated buffer
                            DataInterchange_SelectionSet(current_data, final_fmt,
                                incr->buffer.data(), incr->buffer.size());
                        }

                        if (incr->is_clipboard) {
                            if (callbacks.on_clipboard_receive_data)
                                callbacks.on_clipboard_receive_data(myHandle, current_data);
                        } else {
                            if (callbacks.on_drag_receive_drop)
                                callbacks.on_drag_receive_drop(myHandle, (DragDropData*)current_data);
                            send_xdnd_finished(display, window, incr->requestor, ((DragDropData*)current_data)->status.accept);
                        }

                        if (data) XFree(data);
                        return true;
                    }

                    // First non-empty chunk decides the final type
                    if (incr->first_chunk_type == None) {
                        incr->first_chunk_type = type;
                    }

                    // Append chunk
                    incr->buffer.insert(incr->buffer.end(), data, data + nitems * (format/8));

                    if (data) XFree(data);

                    // Tell owner we’re ready for the next chunk
                    XDeleteProperty(display, incr->requestor, incr->property);
                    return true;
                                       }
                }
        }

        
        
        
        
        
        

    }

    bool CrystalWindow_X11::handle_selection_request(P_INSTANCE(XEvent) event) {
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

        DataInterchange *drag_data;

        if (req->selection == AppX11->atoms.clipboard) {
            std::cerr << "Handling clipboard selection request" << std::endl;
            drag_data = current_clipboard_provide_data;
            // Handle clipboard-specific logic if necessary
        } else if (req->selection == AppX11->atoms.xdnd.selection) {
            std::cerr << "Handling drag-and-drop selection request" << std::endl;
            drag_data = drag_provide->drag_data;
            // Handle drag-and-drop-specific logic if necessary
        } else throw std::runtime_error("Unknow Selection class ");

        utf8_string_struct target = XGetAtomName_struct(req->display, req->target);

        std::cerr << mod_header() << "SelectionRequest for target: " << target << std::endl;

        if (req->target == AppX11->atoms.targets) {
            Atom *types;
            int num_types;

            DataImterchange_AtomArrayFromFormats(drag_data, &types, &num_types);

            XChangeProperty(req->display, req->requestor, req->property, XA_ATOM, 32, PropModeReplace, (P_ELEMENTS(uint8_t) )types, num_types);
        } else if (drag_data != nullptr) {
            utf8_string_struct format = nullptr;

            if (req->target == XInternAtom(req->display, "text/plain", False)) format = "text/plain";
            else if (req->target == XInternAtom(req->display, "text/html", False)) format = "text/html";
            else if (req->target == XInternAtom(req->display, "text/uri-list", False)) format = "text/file-uri";

            std::cerr << mod_header() << "Chosen format: " << format << std::endl;

            drag_data->provide_chosen(drag_data, format);

            P_INSTANCE(void) d;
            size_t sz;
            DataInterchange_SelectionReveal(drag_data, nullptr, &d, &sz);

            std::cerr << mod_header() << "Providing data for format: " << format << std::endl;
            if (strcmp(format, "text/file-uri") == 0) {
                std::string uri_list((char *)d, sz);
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
                XChangeProperty(req->display, req->requestor, req->property, req->target, 8, PropModeReplace, (P_ELEMENTS(uint8_t) )cleaned_uri_list.c_str(), (int) cleaned_uri_list.size());
            } else {
                XChangeProperty(req->display, req->requestor, req->property, req->target, 8, PropModeReplace, (P_ELEMENTS(uint8_t) )d, (int) sz);
            }
        } else {
            std::cerr << mod_header() << "No current_drag_provide_data available" << std::endl;
        }
        XSendEvent(req->display, req->requestor, False, 0, (P_INSTANCE(XEvent) )&ev);

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
        if (handle_property_notify(event)) return true;
        return false;
    }

    void CrystalWindow_X11::DragStart(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
        // Store the DragDropData for later use in SelectionRequest
        if (!drag_provide) { drag_provide = new DragProvide_X11(this); }
        data->m_handle = myHandle;
        data->provide_chosen = DataInterchange::provide_for_drag;

        drag_provide->StartDrag(data, x, y);

    }
}