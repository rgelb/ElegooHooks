#include "elegoo_link.h"
#include "types/event.h"
#include "types/internal/json_serializer.h"

#include <mutex>
#include <string>
#include <type_traits>
#include <variant>

#if defined(_WIN32)
#define EL_EXPORT extern "C" __declspec(dllexport)
#define EL_CALL __cdecl
#else
#define EL_EXPORT extern "C" __attribute__((visibility("default")))
#define EL_CALL
#endif

namespace
{
    using EventCallback = void(EL_CALL *)(const char *json, void *context);

    std::mutex callbackMutex;
    EventCallback eventCallback = nullptr;
    void *eventContext = nullptr;
    bool subscribed = false;
    thread_local std::string returnBuffer;

    const char *setReturn(nlohmann::json value)
    {
        returnBuffer = value.dump();
        return returnBuffer.c_str();
    }

    const char *errorReturn(const std::string &message)
    {
        return setReturn({
            {"code", -1},
            {"message", message},
        });
    }

    template <typename T>
    const char *resultReturn(const elink::BizResult<T> &result)
    {
        nlohmann::json value = {
            {"code", static_cast<int>(result.code)},
            {"message", result.message},
        };

        if constexpr (!std::is_same_v<T, std::monostate>)
        {
            if (result.hasValue())
            {
                value["data"] = result.value();
            }
        }

        return setReturn(std::move(value));
    }

    void emit(const char *type, nlohmann::json data = nlohmann::json::object())
    {
        EventCallback callback;
        void *context;
        {
            std::lock_guard<std::mutex> lock(callbackMutex);
            callback = eventCallback;
            context = eventContext;
        }

        if (callback == nullptr)
        {
            return;
        }

        const std::string envelope = nlohmann::json{
            {"type", type},
            {"data", std::move(data)},
        }.dump();
        callback(envelope.c_str(), context);
    }

    void subscribeToAllEvents()
    {
        if (subscribed)
        {
            return;
        }

        auto &link = elink::ElegooLink::getInstance();
        link.subscribeEvent<elink::PrinterConnectionEvent>(
            [](const std::shared_ptr<elink::PrinterConnectionEvent> &event)
            {
                emit("printer.connection", event->connectionStatus);
            });
        link.subscribeEvent<elink::PrinterStatusEvent>(
            [](const std::shared_ptr<elink::PrinterStatusEvent> &event)
            {
                emit("printer.status", event->status);
            });
        link.subscribeEvent<elink::PrinterAttributesEvent>(
            [](const std::shared_ptr<elink::PrinterAttributesEvent> &event)
            {
                emit("printer.attributes", event->attributes);
            });
        link.subscribeEvent<elink::RtmMessageEvent>(
            [](const std::shared_ptr<elink::RtmMessageEvent> &event)
            {
                emit("rtm.message", event->message);
            });
        link.subscribeEvent<elink::RtcTokenEvent>(
            [](const std::shared_ptr<elink::RtcTokenEvent> &event)
            {
                // Never send the token itself across the logging callback.
                emit("rtc.token.changed", {
                    {"userId", event->token.userId},
                    {"rtcTokenExpireTime", event->token.rtcTokenExpireTime},
                    {"rtcToken", "[redacted]"},
                });
            });
        link.subscribeEvent<elink::LoggedInElsewhereEvent>(
            [](const std::shared_ptr<elink::LoggedInElsewhereEvent> &)
            {
                emit("user.logged.elsewhere");
            });
        link.subscribeEvent<elink::PrinterEventRawEvent>(
            [](const std::shared_ptr<elink::PrinterEventRawEvent> &event)
            {
                emit("printer.raw", event->rawData);
            });
        link.subscribeEvent<elink::PrinterListChangedEvent>(
            [](const std::shared_ptr<elink::PrinterListChangedEvent> &)
            {
                emit("printer.list.changed");
            });
        link.subscribeEvent<elink::OnlineStatusChangedEvent>(
            [](const std::shared_ptr<elink::OnlineStatusChangedEvent> &event)
            {
                emit("user.online.status", {{"isOnline", event->isOnline}});
            });

        subscribed = true;
    }
}

EL_EXPORT int EL_CALL el_initialize(
    int logLevel,
    int enableConsoleLogging,
    EventCallback callback,
    void *context) noexcept
{
    try
    {
        {
            std::lock_guard<std::mutex> lock(callbackMutex);
            eventCallback = callback;
            eventContext = context;
        }

        auto &link = elink::ElegooLink::getInstance();
        if (!link.isInitialized())
        {
            elink::ElegooLink::Config config;
            config.log.logLevel = logLevel;
            config.log.logEnableConsole = enableConsoleLogging != 0;
            config.log.logEnableFile = false;
            if (!link.initialize(config))
            {
                return 0;
            }
        }

        subscribeToAllEvents();
        return 1;
    }
    catch (...)
    {
        return 0;
    }
}

EL_EXPORT const char *EL_CALL el_get_version() noexcept
{
    try
    {
        returnBuffer = elink::ElegooLink::getInstance().getVersion();
        return returnBuffer.c_str();
    }
    catch (const std::exception &exception)
    {
        returnBuffer = std::string("error: ") + exception.what();
        return returnBuffer.c_str();
    }
}

EL_EXPORT const char *EL_CALL el_discover(int timeoutMs) noexcept
{
    try
    {
        elink::PrinterDiscoveryParams parameters;
        parameters.timeoutMs = timeoutMs;
        return resultReturn(elink::ElegooLink::getInstance().startPrinterDiscovery(parameters));
    }
    catch (const std::exception &exception)
    {
        return errorReturn(exception.what());
    }
    catch (...)
    {
        return errorReturn("Unknown native exception during printer discovery.");
    }
}

EL_EXPORT const char *EL_CALL el_connect(const char *optionsJson) noexcept
{
    try
    {
        if (optionsJson == nullptr)
        {
            return errorReturn("Connection options cannot be null.");
        }

        const auto json = nlohmann::json::parse(optionsJson);
        const auto parameters = json.get<elink::ConnectPrinterParams>();
        return resultReturn(elink::ElegooLink::getInstance().connectPrinter(parameters));
    }
    catch (const std::exception &exception)
    {
        return errorReturn(exception.what());
    }
    catch (...)
    {
        return errorReturn("Unknown native exception while connecting to the printer.");
    }
}

EL_EXPORT const char *EL_CALL el_refresh_status(const char *printerId) noexcept
{
    try
    {
        if (printerId == nullptr)
        {
            return errorReturn("Printer ID cannot be null.");
        }

        return resultReturn(elink::ElegooLink::getInstance().refreshPrinterStatus({printerId}));
    }
    catch (const std::exception &exception)
    {
        return errorReturn(exception.what());
    }
    catch (...)
    {
        return errorReturn("Unknown native exception while refreshing printer status.");
    }
}

EL_EXPORT const char *EL_CALL el_disconnect(const char *printerId) noexcept
{
    try
    {
        if (printerId == nullptr)
        {
            return errorReturn("Printer ID cannot be null.");
        }

        return resultReturn(elink::ElegooLink::getInstance().disconnectPrinter(printerId));
    }
    catch (const std::exception &exception)
    {
        return errorReturn(exception.what());
    }
    catch (...)
    {
        return errorReturn("Unknown native exception while disconnecting the printer.");
    }
}

EL_EXPORT void EL_CALL el_cleanup() noexcept
{
    try
    {
        auto &link = elink::ElegooLink::getInstance();
        link.clearAllEventSubscriptions();
        link.cleanup();
        subscribed = false;

        std::lock_guard<std::mutex> lock(callbackMutex);
        eventCallback = nullptr;
        eventContext = nullptr;
    }
    catch (...)
    {
        // Cleanup is best-effort and must not throw across the C ABI.
    }
}
