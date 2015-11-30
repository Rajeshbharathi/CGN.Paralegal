using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LexisNexis.Evolution.Worker
{
    public class FileMimeTypeHelper
    {
        /// <summary>
        /// Method to get the file Mime Type from file extension
        /// </summary>
        /// <param name="fileExt">The file ext.</param>
        /// <returns></returns>
        public string GetMimeType(string fileExt)
        {
            string mimeType = string.Empty;
            switch (fileExt)
            {
                case Constants.File_Ext_Bmp:
                    {
                        mimeType = Constants.MimeType_Bmp;
                        break;
                    }
                case Constants.File_Ext_Doc:
                    {
                        mimeType = Constants.MimeType_Word;
                        break;
                    }
                case Constants.File_Ext_Excel:
                    {
                        mimeType = Constants.MimeType_Excel;
                        break;
                    }
                case Constants.File_Ext_Html:
                    {
                        mimeType = Constants.MimeType_Html;
                        break;
                    }
                case Constants.File_Ext_Jpeg:
                    {
                        mimeType = Constants.MimeType_Jpeg;
                        break;
                    }
                case Constants.File_Ext_Outlook:
                    {
                        mimeType = Constants.MimeType_Outlook;
                        break;
                    }
                case Constants.File_Ext_Pdf:
                    {
                        mimeType = Constants.MimeType_Pdf;
                        break;
                    }
                case Constants.File_Ext_Ppt:
                    {
                        mimeType = Constants.MimeType_Ppt;
                        break;
                    }
                case Constants.File_Ext_Tiff:
                    {
                        mimeType = Constants.MimeType_Tiff;
                        break;
                    }
                case Constants.File_Ext_Txt:
                    {
                        mimeType = Constants.MimeType_Text;
                        break;
                    }
                case Constants.File_Ext_Xml:
                    {
                        mimeType = Constants.MimeType_Xml;
                        break;
                    }
                case Constants.File_Ext_docm:
                    {
                        mimeType = Constants.MimeType_docm;
                        break;
                    }
                case Constants.File_Ext_docx:
                    {
                        mimeType = Constants.MimeType_docx;
                        break;
                    }
                case Constants.File_Ext_dotm:
                    {
                        mimeType = Constants.MimeType_dotm;
                        break;
                    }
                case Constants.File_Ext_dotx:
                    {
                        mimeType = Constants.MimeType_dotx;
                        break;
                    }
                case Constants.File_Ext_potm:
                    {
                        mimeType = Constants.MimeType_potm;
                        break;
                    }
                case Constants.File_Ext_potx:
                    {
                        mimeType = Constants.MimeType_potx;
                        break;
                    }
                case Constants.File_Ext_ppam:
                    {
                        mimeType = Constants.MimeType_ppam;
                        break;
                    }
                case Constants.File_Ext_ppsm:
                    {
                        mimeType = Constants.MimeType_ppsm;
                        break;
                    }
                case Constants.File_Ext_ppsx:
                    {
                        mimeType = Constants.MimeType_ppsx;
                        break;
                    }
                case Constants.File_Ext_pptm:
                    {
                        mimeType = Constants.MimeType_pptm;
                        break;
                    }
                case Constants.File_Ext_pptx:
                    {
                        mimeType = Constants.MimeType_pptx;
                        break;
                    }
                case Constants.File_Ext_xlam:
                    {
                        mimeType = Constants.MimeType_xlam;
                        break;
                    }
                case Constants.File_Ext_xlsb:
                    {
                        mimeType = Constants.MimeType_xlsb;
                        break;
                    }
                case Constants.File_Ext_xlsm:
                    {
                        mimeType = Constants.MimeType_xlsm;
                        break;
                    }
                case Constants.File_Ext_xlsx:
                    {
                        mimeType = Constants.MimeType_xlsx;
                        break;
                    }
                case Constants.File_Ext_xltm:
                    {
                        mimeType = Constants.MimeType_xltm;
                        break;
                    }
                case Constants.File_Ext_xltx:
                    {
                        mimeType = Constants.MimeType_xltx;
                        break;
                    }
                case Constants.File_Ext_zip:
                    {
                        mimeType = Constants.MimeType_Zip;
                        break;
                    }
                case Constants.File_Ext_csv:
                    {
                        mimeType = Constants.MimeType_csv;
                        break;
                    }
                case Constants.File_Ext_rtf:
                    {
                        mimeType = Constants.MimeType_rtf;
                        break;
                    }
                case Constants.File_Ext_Unknown:
                    {
                        mimeType = Constants.MimeType_OpenXml;
                        break;
                    }
            }
            return mimeType;
        }
    }
}
