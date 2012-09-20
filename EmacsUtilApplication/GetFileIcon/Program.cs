using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Drawing.Imaging;

namespace GetFileIcon
{
    class Program
    {
        static void Main(string[] args)
        {

            string filename = args[0];
            string outputFolder = args[1];

            string iconFolder = null;
            string iconFile = null;

            Icon icon = null;

            SHA1 sha1 = new SHA1CryptoServiceProvider();

            FileAttributes attr = File.GetAttributes(filename);

            if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
            {
                Dictionary<string, string> iconsInfo = RegisteredFileType.GetFileTypeAndIcon();

                string fileExtension = Path.GetExtension(filename);
                byte[] bs = sha1.ComputeHash(Encoding.Unicode.GetBytes(fileExtension));

                string shaString = BitConverter.ToString(bs).ToLower().Replace("-", "");
                iconFolder = outputFolder + Path.DirectorySeparatorChar + shaString.Substring(0, 2);
                iconFile = iconFolder + Path.DirectorySeparatorChar + shaString.Substring(2);

                if (iconsInfo.ContainsKey(fileExtension))
                {
                    icon = RegisteredFileType.ExtractIconFromFile(iconsInfo[fileExtension], false);
                }
            }

            if (icon == null)
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hImgSmall = Win32.SHGetFileInfo(filename, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON);
                icon = System.Drawing.Icon.FromHandle(shinfo.hIcon);

                MemoryStream stream = new MemoryStream();
                icon.ToBitmap().Save(stream, ImageFormat.Bmp);
                byte[] bs = sha1.ComputeHash(stream.GetBuffer());

                string shaString = BitConverter.ToString(bs).ToLower().Replace("-", "");
                iconFolder = outputFolder + Path.DirectorySeparatorChar + shaString.Substring(0, 2);
                iconFile = iconFolder + Path.DirectorySeparatorChar + shaString.Substring(2);
            }

            if (!Directory.Exists(iconFolder))
            {
                Directory.CreateDirectory(iconFolder);
            }

            if (!File.Exists(iconFile) && icon != null)
            {
                icon.ToBitmap().Save(iconFile);
            }

            Console.WriteLine(iconFile);

            //Console.ReadLine();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon  
        public const uint SHGFI_SMALLICON = 0x1; // 'Small icon  

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
    }
}
