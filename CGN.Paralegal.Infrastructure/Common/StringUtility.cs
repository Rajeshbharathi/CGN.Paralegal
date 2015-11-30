#region File Header
//---------------------------------------------------------------------------------------------------
// <copyright file="StringUtility.cs" company="Cognizant">
//      Copyright (c) Cognizant Technology Solutions. All rights reserved.
// </copyright>
// <header>
//      <author>Ravi</author>
//      <description>
//          Holds the StringUtility object
//      </description>
//      <changelog>
//          <date value="Oct-11-2012">Added GetHashWithSalt() - babugx</date>
//          <date value="Oct-17-2012">Modified GetHashWithSalt() to treat with lower case - babugx</date>
//          <date value="Feb-7-2014">Modified EncodeTo64() and DecodeFrom64()-  Convert ASCIIEncoding  to UTF8Encoding  - babugx</date>
//      </changelog>
// </header>
//---------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CGN.Paralegal.Infrastructure.Common
{
    /// <summary>
    /// This class is used for constructing search query in search sub-system and helper methods
    /// </summary>
    public sealed class StringUtility
    {
        private StringUtility()
        {
        }


        /// <summary>
        /// checks if the string is null or empty or having just white space
        /// </summary>
        /// <param name="value">string value</param>
        /// <returns>true if either the value is null/ empty/ white space</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return (String.IsNullOrEmpty(value) || String.IsNullOrEmpty(value.Trim()));
        }

        /// <summary>
        /// Checks for null and return empty when it is null other wise return original value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetValue(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        /// <summary>
        /// Encodes the to64.
        /// </summary>
        /// <param name="toEncode">To encode.</param>
        /// <returns>Encoded string</returns>
        public static string EncodeTo64(string toEncode)
        {
            if (null == toEncode)
            {
                return null;
            }
            var encoding = new UTF8Encoding();
            byte[] toEncodeAsBytes = encoding.GetBytes(toEncode);
            string returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public static string DecodeFrom64(string encodedData)
        {
            if (null == encodedData)
            {
                return null;
            }
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            var encoding = new UTF8Encoding();
            string returnValue = encoding.GetString(encodedDataAsBytes);
            return returnValue;

        }
        #region Hashing
        /// <summary>
        /// Gets Hash string of a given text
        /// </summary>
        /// <param name="text">Text to compute hash</param>
        /// <returns>Computed Hash string</returns>
        public static string GetSHA1(string text)
        {
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] hashValue; byte[] message = unicode.GetBytes(text);
            using (SHA1Managed hashString = new SHA1Managed())
            {
                hashValue = hashString.ComputeHash(message);
                return hashValue.Aggregate("", (current, x) => current + String.Format("{0:x2}", x));
            }
        }

        /// <summary>
        /// Gets Hash string of a given text
        /// </summary>
        /// <param name="text">Text to compute hash</param>
        /// <returns>Computed Hash string</returns>
        public static string GetMD5(string text)
        {
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] message = unicode.GetBytes(text);
            using (MD5CryptoServiceProvider hashString = new MD5CryptoServiceProvider())
            {
                byte[] hashValue = hashString.ComputeHash(message);
                return hashValue.Aggregate("", (current, x) => current + String.Format("{0:x2}", x));
            }
        }

        /// <summary>
        /// Gets HAsh of the string of a given text with an predefined SALT
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>string</returns>
        public static string GetHashWithSalt(string text)
        {
            string saltKey = "CHEV2.1";
            HashAlgorithm algo = new SHA256Managed();

            byte[] textBytes = Encoding.UTF8.GetBytes(text.ToLower());
            byte[] saltKeyBytes = Encoding.UTF8.GetBytes(saltKey.ToLower());
            byte[] plainTextWithSaltBytes = new byte[textBytes.Length + saltKeyBytes.Length];

            for (int i = 0; i < textBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = textBytes[i];
            }

            for (int i = 0; i < saltKeyBytes.Length; i++)
            {
                plainTextWithSaltBytes[textBytes.Length + i] = saltKeyBytes[i];
            }

            return Convert.ToBase64String(algo.ComputeHash(plainTextWithSaltBytes));
        }

        #endregion

        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        public static string SanitizeXmlString(string content)
        {
            if (String.IsNullOrEmpty(content))
            {
                return String.Empty;
            }

            var buffer = new StringBuilder(content.Length);

            foreach (char c in content)
            {
                buffer.Append(IsLegalXmlChar(c) ? c : ' ');
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        public static bool IsLegalXmlChar(int character)
        {
            return
            (
                 character == 0x9 /* == '\t' == 9   */          ||
                 character == 0xA /* == '\n' == 10  */          ||
                 character == 0xD /* == '\r' == 13  */          ||
                (character >= 0x20 && character <= 0xD7FF) ||
                (character >= 0xE000 && character <= 0xFFFD) ||
                (character >= 0x10000 && character <= 0x10FFFF)
            );
        }

    }
}
