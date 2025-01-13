#include <windows.h>
#include "detours.h"

static void (WINAPI* GetStartupInfoA_real)(LPSTARTUPINFOA) = GetStartupInfoA;
static void GetStartupInfoA_detoured(LPSTARTUPINFOA lpStartupInfo)
{
    static bool injected = false;

    if (!injected)
    {
        SuspendThread(GetCurrentThread()); //suspend the thread again now that we are ready to inject
        injected = true;
    }

    return GetStartupInfoA_real(lpStartupInfo);
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    if (DetourIsHelperProcess())
    {
        return TRUE;
    }

    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        DetourRestoreAfterWith();
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourAttach(&(PVOID&)GetStartupInfoA_real, (PVOID)GetStartupInfoA_detoured);
        DetourTransactionCommit();
    }
    else if (ul_reason_for_call == DLL_PROCESS_DETACH)
    {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourDetach(&(PVOID&)GetStartupInfoA_real, (PVOID)GetStartupInfoA_detoured);
        DetourTransactionCommit();
    }

    return TRUE;
}

