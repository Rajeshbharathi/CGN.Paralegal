# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="DocumentHashHelper.cs" company="LexisNexis">
//      Copyright (c) LexisNexis. All rights reserved.
// </copyright>
// <header>
//      <author>Srini</author>
//      <description>
//          Document HashHelper class
//      </description>
//      <changelog>
//          <date value="06/0/2012">Task # 101476(CR BVT Fixes)</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion
using System;
using System.IO;
using System.Security.Cryptography;

namespace LexisNexis.Evolution.DocumentImportUtilities
{
    /// <summary>
    /// Helper class used to calculate Hash value
    /// </summary>
    public static class DocumentHashHelper
    {
        public static string GetMD5HashValue(string filePath)
        {
            string mdHashValue;
            using (MD5 md5 = new MD5CryptoServiceProvider())
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] retVal = md5.ComputeHash(file);
                mdHashValue= BitConverter.ToString(retVal).Replace("-", "");	// hex string
            }
            return mdHashValue;
        }

        /// <summary>
        /// Calculate hash value using SHA
        /// </summary> 
        public static string GetSHAHashValue(string filePath)
        {
            string shahHashValue;
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] retVal = sha1.ComputeHash(file);
                shahHashValue= BitConverter.ToString(retVal).Replace("-", "");	// hex string
            }
            return shahHashValue;
        }
    }
}
