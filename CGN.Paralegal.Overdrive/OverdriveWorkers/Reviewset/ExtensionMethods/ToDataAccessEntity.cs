# region File Header
//-----------------------------------------------------------------------------------------
// <copyright file="ToBusinessEntityExtension.cs" company="Lexis Nexis">
//      Copyright (c) Lexis Nexis. All rights reserved.
// </copyright>
// <header>
//      <author>nagaraju/raj kumar/ganesh</author>
//      <description>
//          This is a file that contains Business Entity Extension  class 
//      </description>
//      <changelog>
//          <date value="15-March-2011"></date>
//          <date value="02-05-2011">Removed IsDisableRichText proterty</date>
//          <date value="02-06-2011">Review Set Beo changes for performance improvement</date>
//          <date value="03/02/2012">Bug Fix 86335</date>
//	        <date value="03/01/2012">Fix for bug 86129</date>
//      </changelog>
// </header>
//-----------------------------------------------------------------------------------------
# endregion

#region Namespace

using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using LexisNexis.Evolution.Worker.Data;

#endregion

namespace LexisNexis.Evolution.Worker
{
    public static class ToDataAccessEntityExtension
    {

        public static List<FieldRecord> ToDataAccessEntity(this List<DocumentField>  lstDocumentFields)
        {
            List<FieldRecord> lstFieldRecords = new List<FieldRecord>();
            if (lstDocumentFields != null && lstDocumentFields.Any())
            {
                lstDocumentFields.SafeForEach(docField => lstFieldRecords.Add(new FieldRecord() { FieldName = docField.FieldName, FieldValue = docField.Value }));
            }
            return lstFieldRecords;
        }
   
    }
}
