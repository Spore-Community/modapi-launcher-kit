#include <cassert>
#include <cstdint>
#include <string>
#include <vector>
#include <windows.h>
#include "detours.h"

static uintptr_t InitializeGeneralAllocator_disk_address = 0x01390ca0;
static uintptr_t InitializeGeneralAllocator_address = 0x0138e640;

static uintptr_t DisposeGeneralAllocator_disk_address = 0x013cc060;
static uintptr_t DisposeGeneralAllocator_address = 0x013c9950;

#define GetFunctionPtr(name, is_disk) reinterpret_cast<decltype(name##_real)>((uintptr_t)GetModuleHandleA(nullptr) + (is_disk ? name##_disk_address : name##_address) - 0x400000)

static bool is_disk_spore = false;
static std::vector<std::wstring> dlls;
static std::vector<HMODULE> dll_handles;

static void (__stdcall* InitializeGeneralAllocator_real)() = nullptr;
static void __stdcall InitializeGeneralAllocator_detoured()
{
	InitializeGeneralAllocator_real();

    for (const auto& dll : dlls)
    {
	    dll_handles.push_back(LoadLibraryW(dll.c_str()));
    }
    dlls.clear();
}

static void (__stdcall* DisposeGeneralAllocator_real)() = nullptr;
static void __stdcall DisposeGeneralAllocator_detoured()
{
    for (size_t i = dll_handles.size(); i > 0; --i)
    {
        FreeLibrary(dll_handles[i - 1]);
    }
    dll_handles.clear();

	DisposeGeneralAllocator_real();
}

static void (WINAPI* GetStartupInfoA_real)(LPSTARTUPINFOA) = GetStartupInfoA;
static void GetStartupInfoA_detoured(LPSTARTUPINFOA lpStartupInfo)
{
    static bool injected = false;

    if (!injected)
    {
        injected = true;
        InitializeGeneralAllocator_real = GetFunctionPtr(InitializeGeneralAllocator, is_disk_spore);
        DisposeGeneralAllocator_real = GetFunctionPtr(DisposeGeneralAllocator, is_disk_spore);

        DetourRestoreAfterWith();
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourAttach(&(PVOID&)InitializeGeneralAllocator_real, (PVOID)InitializeGeneralAllocator_detoured);
        DetourAttach(&(PVOID&)DisposeGeneralAllocator_real, (PVOID)DisposeGeneralAllocator_detoured);
        DetourTransactionCommit();
    }

    return GetStartupInfoA_real(lpStartupInfo);
}

void APIENTRY SetInjectionData(const uint8_t* data)
{
    #pragma comment(linker, "/EXPORT:" __FUNCTION__"=" __FUNCDNAME__)
    int data_offset = 0;
    is_disk_spore = data[data_offset++] == 1;
    uint32_t num_dlls = *reinterpret_cast<const uint32_t*>(data + data_offset);
    data_offset += sizeof(num_dlls);
    for (uint32_t i = 0; i < num_dlls; ++i)
    {
        uint32_t num_str_bytes = *reinterpret_cast<const uint32_t*>(data + data_offset);
		data_offset += sizeof(num_str_bytes);
        const auto str_ptr = reinterpret_cast<const wchar_t*>(data + data_offset);
        data_offset += static_cast<int>(num_str_bytes * sizeof(wchar_t));

        dlls.emplace_back(str_ptr, num_str_bytes);
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    if (DetourIsHelperProcess())
    {
        return TRUE;
    }

    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        //MessageBoxA(NULL, "attach debugger", "spore", MB_OK | MB_ICONERROR);
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
        if (InitializeGeneralAllocator_real) DetourDetach(&(PVOID&)InitializeGeneralAllocator_real, (PVOID)InitializeGeneralAllocator_detoured);
        if (DisposeGeneralAllocator_real) DetourDetach(&(PVOID&)DisposeGeneralAllocator_real, (PVOID)DisposeGeneralAllocator_detoured);
        DetourTransactionCommit();
    }

    return TRUE;
}

