@symbolic_object
{

    Destination : "Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net";
    LibraryName : "CrystalCatalystLibrary";

    ContextTypes : [
        CrystalWindow:WindowHandle,
        DataInterchange:DataInterchange,
        DragDropData:DragDropData,
        SubMutex: @param spiderMutex
    ];

    Inherits : [
        "DragDropData" : "DataInterchange"
    ];

    ImportTypeMap : [
        "utf8_string_struct": "ref utf8_string_struct",
        "WindowHandle": "IntPtr",
        "int32_t": "int",
        "size_t": "IntPtr",
        "DataInterchange::Node": "IntPtr"
    ];

    DefineTypeMap : [
        "utf8_string_struct" : "string",
        "WindowHandle": "CrystalWindow",
        "int32_t": "int",
        "size_t": "IntPtr",
        "DataInterchange::Node": "IntPtr"
    ];

    Directives : {
        WriteCallbacks("CrystalWindow", "
            void (*on_draw)(P_INSTANCE(WindowHandle) window_handle);

            void (*on_key_down)(P_INSTANCE(WindowHandle) window_handle, int32_t keycode);
            void (*on_key_up)(P_INSTANCE(WindowHandle) window_handle, int32_t keycode);

            void (*on_mouse_move)(P_INSTANCE(WindowHandle) window_handle, int32_t x, int32_t y);
            void (*on_mouse_down)(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y);
            void (*on_mouse_up)(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y);

            void (*on_resize)(P_INSTANCE(WindowHandle) window_handle, int32_t width, int32_t height);

            void (*on_close)(P_INSTANCE(WindowHandle) window_handle);

            void (*on_focus_in)(P_INSTANCE(WindowHandle) window_handle);
            void (*on_focus_out)(P_INSTANCE(WindowHandle) window_handle);

            void (*on_drag_receive_start)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);

            void (*on_drag_receive_enter)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
            void (*on_drag_receive_motion)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y, uint32_t key_state);
            void (*on_drag_receive_leave)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
            void (*on_drag_receive_drop)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
            utf8_string_struct (*on_drag_receive_select)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);

            void (*on_drag_provide_status)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
            void (*on_drag_provide_chosen)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, utf8_string_struct format);
            void (*on_drag_provide_finished)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data, bool success);

            void (*on_clipboard_provide_chosen)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_struct format);
            void (*on_clipboard_receive_data)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data);

            void (*on_idle)(P_INSTANCE(WindowHandle) window_handle);
        ");
    }

}