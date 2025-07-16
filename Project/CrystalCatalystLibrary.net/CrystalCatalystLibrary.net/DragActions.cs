namespace CrystalCatalystLibrary;

public enum DragActions {
    DRAG_OPERATION_NONE = 0,
    DRAG_OPERATION_COPY = 1 << 0,
    DRAG_OPERATION_MOVE = 1 << 1,
    DRAG_OPERATION_LINK = 1 << 2
};
