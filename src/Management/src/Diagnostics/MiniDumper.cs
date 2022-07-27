// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.Diagnostics;

internal class MiniDumper
{
    public struct Result
    {
        public int ReturnValue;
        public int ErrorCode;
        public Exception Exception;
    }

    internal static Result DumpProcess(FileStream dumpFile, IntPtr processHandle, int pid)
    {
        IntPtr callbackParam = default;
        Result result = default;
        GCHandle callbackHandle = default;
        try
        {
            var callbackDelegate = new MiniDumpCallback(MiniDumpCallbackMethod);
            callbackHandle = GCHandle.Alloc(callbackDelegate);
            callbackParam = Marshal.AllocHGlobal(IntPtr.Size * 2);

            unsafe
            {
                var ptr = (MINIDUMP_CALLBACK_INFORMATION*)callbackParam;
                ptr->CallbackRoutine = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
                ptr->CallbackParam = IntPtr.Zero;
            }

#pragma warning disable S3869 // "SafeHandle.DangerousGetHandle" should not be called
            var fileHandle = dumpFile.SafeFileHandle.DangerousGetHandle();
#pragma warning restore S3869 // "SafeHandle.DangerousGetHandle" should not be called

            result.ReturnValue = MiniDumpWriteDump(processHandle, pid, fileHandle, GetMiniDumpType(), IntPtr.Zero, IntPtr.Zero, callbackParam);
            result.ErrorCode = Marshal.GetHRForLastWin32Error();
        }
        catch (Exception e)
        {
            result.ErrorCode = Marshal.GetHRForLastWin32Error();
            result.ReturnValue = 0;
            result.Exception = e;
        }
        finally
        {
            if (callbackParam != default)
            {
                Marshal.FreeHGlobal(callbackParam);
            }

            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
        }

        return result;
    }

    internal static MINIDUMP_TYPE GetMiniDumpType()
    {
        var minidumpFlags = MINIDUMP_TYPE.MiniDumpWithDataSegs |
                            MINIDUMP_TYPE.MiniDumpWithTokenInformation |
                            MINIDUMP_TYPE.MiniDumpWithPrivateWriteCopyMemory |
                            MINIDUMP_TYPE.MiniDumpWithPrivateReadWriteMemory |
                            MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                            MINIDUMP_TYPE.MiniDumpWithFullMemory |
                            MINIDUMP_TYPE.MiniDumpWithHandleData |
                            MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                            MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                            MINIDUMP_TYPE.MiniDumpWithProcessThreadData |
                            MINIDUMP_TYPE.MiniDumpWithModuleHeaders;
        return minidumpFlags;
    }

    internal struct MINIDUMP_CALLBACK_INFORMATION
    {
        public IntPtr CallbackRoutine;
        public IntPtr CallbackParam;
    }

    internal enum MINIDUMP_CALLBACK_TYPE : uint
    {
        ModuleCallback,
        ThreadCallback,
        ThreadExCallback,
        IncludeThreadCallback,
        IncludeModuleCallback,
        MemoryCallback,
        CancelCallback,
        WriteKernelMinidumpCallback,
        KernelMinidumpStatusCallback,
        RemoveMemoryCallback,
        IncludeVmRegionCallback,
        IoStartCallback,
        IoWriteAllCallback,
        IoFinishCallback,
        ReadMemoryFailureCallback,
        SecondaryFlagsCallback,
        IsProcessSnapshotCallback,
        VmStartCallback,
        VmQueryCallback,
        VmPreReadCallback,
    }

    [Flags]
    internal enum MINIDUMP_TYPE : uint
    {
        MiniDumpNormal = 0x00000000,
        MiniDumpWithDataSegs = 0x00000001,
        MiniDumpWithFullMemory = 0x00000002,
        MiniDumpWithHandleData = 0x00000004,
        MiniDumpFilterMemory = 0x00000008,
        MiniDumpScanMemory = 0x00000010,
        MiniDumpWithUnloadedModules = 0x00000020,
        MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
        MiniDumpFilterModulePaths = 0x00000080,
        MiniDumpWithProcessThreadData = 0x00000100,
        MiniDumpWithPrivateReadWriteMemory = 0x00000200,
        MiniDumpWithoutOptionalData = 0x00000400,
        MiniDumpWithFullMemoryInfo = 0x00000800,
        MiniDumpWithThreadInfo = 0x00001000,
        MiniDumpWithCodeSegs = 0x00002000,
        MiniDumpWithoutAuxiliaryState = 0x00004000,
        MiniDumpWithFullAuxiliaryState = 0x00008000,
        MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
        MiniDumpIgnoreInaccessibleMemory = 0x00020000,
        MiniDumpWithTokenInformation = 0x00040000,
        MiniDumpWithModuleHeaders = 0x00080000,
        MiniDumpFilterTriage = 0x00100000,
    }

    internal struct MINIDUMP_CALLBACK_OUTPUT
    {
        public int Status; // HRESULT
    }

    [DllImport("DbgHelp", SetLastError = true)]

    private static extern int MiniDumpWriteDump(IntPtr processHandle, int processId, IntPtr fileHandle, MINIDUMP_TYPE dumpType, IntPtr excepParam, IntPtr userParam, IntPtr callParam);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int MiniDumpCallback(IntPtr callbackParam, IntPtr callbackInput, IntPtr callbackOutput);

    internal static int MiniDumpCallbackMethod(IntPtr param, IntPtr input, IntPtr output)
    {
        unsafe
        {
            if (Marshal.ReadByte(input + sizeof(int) + IntPtr.Size) == (int)MINIDUMP_CALLBACK_TYPE.IsProcessSnapshotCallback)
            {
                var o = (MINIDUMP_CALLBACK_OUTPUT*)output;

                // removed null check on o because sonar analyzer indicates that o will never be null... TH - 7/2/2019
                o->Status = 1;
            }
        }

        return 1;
    }
}