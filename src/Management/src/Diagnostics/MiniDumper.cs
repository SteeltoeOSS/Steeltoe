// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Steeltoe.Management.Diagnostics;

internal sealed class MiniDumper
{
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
                var ptr = (MiniDumpCallbackInformation*)callbackParam;
                ptr->CallbackRoutine = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
                ptr->CallbackParam = IntPtr.Zero;
            }

#pragma warning disable S3869 // "SafeHandle.DangerousGetHandle" should not be called
            IntPtr fileHandle = dumpFile.SafeFileHandle.DangerousGetHandle();
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

    internal static MiniDumpTypes GetMiniDumpType()
    {
        const MiniDumpTypes minidumpFlags = MiniDumpTypes.MiniDumpWithDataSegs | MiniDumpTypes.MiniDumpWithTokenInformation |
            MiniDumpTypes.MiniDumpWithPrivateWriteCopyMemory | MiniDumpTypes.MiniDumpWithPrivateReadWriteMemory | MiniDumpTypes.MiniDumpWithUnloadedModules |
            MiniDumpTypes.MiniDumpWithFullMemory | MiniDumpTypes.MiniDumpWithHandleData | MiniDumpTypes.MiniDumpWithThreadInfo |
            MiniDumpTypes.MiniDumpWithFullMemoryInfo | MiniDumpTypes.MiniDumpWithProcessThreadData | MiniDumpTypes.MiniDumpWithModuleHeaders;

        return minidumpFlags;
    }

    [DllImport("DbgHelp", SetLastError = true)]
    private static extern int MiniDumpWriteDump(IntPtr processHandle, int processId, IntPtr fileHandle, MiniDumpTypes dumpType, IntPtr exceptionParam,
        IntPtr userParam, IntPtr callParam);

    internal static int MiniDumpCallbackMethod(IntPtr param, IntPtr input, IntPtr output)
    {
        unsafe
        {
            if (Marshal.ReadByte(input + sizeof(int) + IntPtr.Size) == (int)MiniDumpCallbackType.IsProcessSnapshotCallback)
            {
                var o = (MinidumpCallbackOutput*)output;

                // removed null check on o because sonar analyzer indicates that o will never be null... TH - 7/2/2019
                o->Status = 1;
            }
        }

        return 1;
    }

    public struct Result
    {
        public int ReturnValue;
        public int ErrorCode;
        public Exception Exception;
    }

    internal struct MiniDumpCallbackInformation
    {
        public IntPtr CallbackRoutine;
        public IntPtr CallbackParam;
    }

    internal enum MiniDumpCallbackType : uint
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
        VmPreReadCallback
    }

    [Flags]
    internal enum MiniDumpTypes : uint
    {
#pragma warning disable S2346 // Flags enumerations zero-value members should be named "None"
        MiniDumpNormal = 0x00000000,
#pragma warning restore S2346 // Flags enumerations zero-value members should be named "None"
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
        MiniDumpFilterTriage = 0x00100000
    }

    internal struct MinidumpCallbackOutput
    {
        public int Status; // HRESULT
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int MiniDumpCallback(IntPtr callbackParam, IntPtr callbackInput, IntPtr callbackOutput);
}
