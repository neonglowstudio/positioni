using System.Runtime.InteropServices;
using System.Security.Cryptography;


namespace Positioni;

internal static class Authenticode
{
    private static readonly Guid VerifyAction = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    internal static byte[]? TrustedSigner(string path)
    {
        var file = new FileInfoNative
        {
            Size = (uint)Marshal.SizeOf<FileInfoNative>(),
            Path = path
        };
        nint pointer = Marshal.AllocHGlobal(Marshal.SizeOf<FileInfoNative>());
        var data = new TrustData
        {
            Size = (uint)Marshal.SizeOf<TrustData>(),
            UiChoice = 2,
            RevocationChecks = 1,
            UnionChoice = 1,
            File = pointer,
            StateAction = 1,
            ProviderFlags = 0x80
        };
        var action = VerifyAction;
        Marshal.StructureToPtr(file, pointer, false);
        try
        {
            if (WinVerifyTrust(new nint(-1), ref action, ref data) != 0 || data.StateData == 0) return null;
            nint provider = WTHelperProvDataFromStateData(data.StateData);
            if (provider == 0) return null;
            nint signer = WTHelperGetProvSignerFromChain(provider, 0, false, 0);
            if (signer == 0) return null;
            nint leaf = WTHelperGetProvCertFromChain(signer, 0);
            if (leaf == 0) return null;
            nint context = Marshal.PtrToStructure<ProviderCert>(leaf).Context;
            if (context == 0) return null;
            var certificate = Marshal.PtrToStructure<CertificateContext>(context);
            if (certificate.Encoded == 0 || certificate.Length is 0 or > 1_000_000) return null;
            var bytes = new byte[certificate.Length];
            Marshal.Copy(certificate.Encoded, bytes, 0, bytes.Length);
            return SHA256.HashData(bytes);
        }
        finally
        {
            if (data.StateData != 0)
            {
                data.StateAction = 2;
                WinVerifyTrust(new nint(-1), ref action, ref data);
            }
            Marshal.DestroyStructure<FileInfoNative>(pointer);
            Marshal.FreeHGlobal(pointer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProviderCert
    {
        public uint Size;
        public nint Context;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CertificateContext
    {
        public uint Encoding;
        public nint Encoded;
        public uint Length;
    }

    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern nint WTHelperProvDataFromStateData(nint state);

    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern nint WTHelperGetProvSignerFromChain(nint provider, uint signerIndex,
        [MarshalAs(UnmanagedType.Bool)] bool counterSigner, uint counterSignerIndex);

    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern nint WTHelperGetProvCertFromChain(nint signer, uint certificateIndex);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FileInfoNative
    {
        public uint Size;
        [MarshalAs(UnmanagedType.LPWStr)] public string Path;
        public nint FileHandle, KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrustData
    {
        public uint Size;
        public nint PolicyData, ClientData;
        public uint UiChoice, RevocationChecks, UnionChoice;
        public nint File;
        public uint StateAction;
        public nint StateData, UrlReference;
        public uint ProviderFlags, UiContext;
        public nint SignatureSettings;
    }

    [DllImport("wintrust.dll", ExactSpelling = true)]
    private static extern int WinVerifyTrust(nint hwnd, ref Guid action, ref TrustData data);
}
