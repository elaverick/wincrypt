using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinCrypt
{
    /// <summary>
    /// Supporting class to contain details for encrypted files
    /// </summary>
    public class fileDetails
    {
        private string _filename;
        private string _fullname;
        private long _length;
        private Icon _fileIcon;

        public string filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public string fullname
        {
            get { return _fullname; }
            set { _fullname = value; }
        }

        public ImageSource fileIcon
        {
            get
            {
                if (_fileIcon == null)
                    return null;
                Bitmap bitmap = _fileIcon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();

                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        public long length
        {
            get { return _length; }
            set { _length = value; }
        }

        public fileDetails(FileInfo info, Icon icon)
        {
            _filename = info.Name;
            _fullname = info.FullName;
            _length = info.Length;
            _fileIcon = icon;
        }

        public fileDetails(string filename, long length)
        {
            _filename = filename;
            _fullname = filename;
            _length = length;
            _fileIcon = null;
        }

        public fileDetails(string filename, long length, Icon icon)
        {
            _filename = filename;
            _fullname = filename;
            _length = length;
            _fileIcon = icon;
        }

    }
}
