#ifndef ACCESSIBILITY_INTERFACE_H
#define ACCESSIBILITY_INTERFACE_H

#ifdef _WIN32
    #include <windows.h>
    #include <UIAutomation.h>
#elif __linux__
    #include <atspi/atspi.h>
#endif

#include <string>
#include <memory>

// Abstract base class for Accessibility Interfaces
class AccessibilityInterface {
public:
    virtual ~AccessibilityInterface() = default;

    // Initializes the accessibility framework
    virtual bool Initialize() = 0;

    // Creates an accessible element
    virtual bool CreateAccessibleElement(const std::string& elementId, const std::string& role, const std::string& name) = 0;

    // Sets a property for an accessible element
    virtual bool SetElementProperty(const std::string& elementId, const std::string& property, const std::string& value) = 0;

    // Sends an event for an accessible element
    virtual bool SendAccessibilityEvent(const std::string& elementId, const std::string& eventType) = 0;

    // Finalizes the accessibility framework
    virtual void Finalize() = 0;
};

// Factory function to create the appropriate accessibility interface
std::unique_ptr<AccessibilityInterface> CreateAccessibilityInterface();

#endif // ACCESSIBILITY_INTERFACE_H
