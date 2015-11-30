using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;
using LexisNexis.Evolution.Business;
using LexisNexis.Evolution.BusinessEntities;
using LexisNexis.Evolution.BusinessEntities.Law;
using LexisNexis.Evolution.External.Law;
using LexisNexis.Evolution.Infrastructure;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.Services.Test
{
    public partial class LawAdapter : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnSyncMetadata_Click(object sender, EventArgs e)
        {
            try
            {
                string lawIniFile = txtLawIniFile.Text;
                List<string> fieldNames = SplitStr(txtFieldNames.Text);
                List<string> fieldTypes = SplitStr(txtFieldTypes.Text);
                List<string> fieldValues = SplitStr(txtFieldValues.Text);
                List<string> tagNames = SplitStr(txtTagNames.Text);
                List<string> tagValues = SplitStr(txtTagValues.Text);
                IEVLawAdapter adapter = EVLawAdapterFactory.NewEVLawAdapter(lawIniFile, "law_user", "law32sql");

                string[] range = txtLawDocIDRange.Text.Split(" -".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                int lawDocIdBegin = Convert.ToInt32(range[0]);
                int lawDocIdEnd = Convert.ToInt32(range[1]);
                int fieldCount = fieldNames.Count;
                int tagCount = tagNames.Count;
                int i;
                List<LawMetadataBEO> metadatas = new List<LawMetadataBEO>();
                for (i = 0; i < fieldCount; i++)
                {
                    string inputFieldValue = fieldValues[i];
                    LawFieldTypeBEO lawFieldType;
                    Object fieldValue;
                    bool isFieldValueNull = "null".Equals(inputFieldValue, StringComparison.InvariantCultureIgnoreCase);


                    switch (fieldTypes[i])
                    {
                        case "string":
                            lawFieldType = LawFieldTypeBEO.String;
                            fieldValue = isFieldValueNull ? null : inputFieldValue;
                            break;
                        case "int":
                            lawFieldType = LawFieldTypeBEO.Numeric;
                            fieldValue = isFieldValueNull ? null : (Object) Convert.ToInt32(inputFieldValue);
                            break;
                        case "date":
                            lawFieldType = LawFieldTypeBEO.DateTime;
                            fieldValue = isFieldValueNull
                                ? null
                                : (Object) DateTime.ParseExact(inputFieldValue, "yyyy-MM-dd HH:mm:ss",
                                    CultureInfo.CurrentCulture);
                            break;
                        default:
                            throw new Exception("Invalid fieldType: " + fieldTypes[i]);
                    }



                    var metadata = new LawMetadataBEO
                                   {
                                       IsTag = false,
                                       Name = fieldNames[i],
                                       Type = lawFieldType,
                                       Value = fieldValue
                                   };
                    metadatas.Add(metadata);
                    adapter.CreateField(fieldNames[i], lawFieldType);
                }

                for (i = 0; i < tagCount; i++)
                {
                    var metadata = new LawMetadataBEO
                                   {
                                       IsTag = true,
                                       Name = tagNames[i],
                                       Value =
                                           tagValues[i].Equals("true", StringComparison.OrdinalIgnoreCase)
                                               ? true
                                               : false
                                   };
                    metadatas.Add(metadata);
                    adapter.CreateTag(tagNames[i]);
                }
                var lawDocs = new List<LawDocumentBEO>();
                for (int docId = lawDocIdBegin; docId <= lawDocIdEnd; docId++)
                {
                    var lawDoc = new LawDocumentBEO()
                                 {
                                     LawDocId = docId,
                                     LawMetadatas = CloneLawMetadataList(metadatas)
                                 };
                    lawDocs.Add(lawDoc);
                }

                List<string> metadataNames = new List<string>();
                metadataNames.AddRange(fieldNames);
                metadataNames.AddRange(tagNames);
                // fieldNames.SafeForEach(o => adapter.CreateField(o, LawFieldTypeBEO.Numeric));
                // tagNames.SafeForEach(o => adapter.CreateTag(o));

                adapter.UpdateLawMetadata(lawDocs, metadataNames);
                ltlResult.Text = "Law Sync Success";

            }
            catch (Exception ex)
            {
                ltlResult.Text = string.Format("Error Code: {0}\r\nUser Message: {1}\r\nDebug Message: {2}\r\n{3}", ex.GetErrorCode(),
                    ex.ToUserString(), ex.ToDebugString(), ex.ToString());

            }
        }

        
        private List<LawMetadataBEO> CloneLawMetadataList(List<LawMetadataBEO> a)
        {
            var ret = a.Select(o => CloneLawMetadata(o));
            return ret.ToList();


        }

        private static LawMetadataBEO CloneLawMetadata(LawMetadataBEO o)
        {
            var ret = new LawMetadataBEO()
                      {
                          Id = o.Id,
                          IsTag = o.IsTag,
                          Name = o.Name,
                          Type = o.Type,
                          Value = o.Value
                      };
            return ret;
        }
        private static List<string> SplitStr(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return new List<string>();
            }
            return s.Split(",".ToCharArray()).Where(o => !string.IsNullOrEmpty(o)).Select(o => o.Trim()).ToList();
        }

        private static List<string> SplitStr(string s, char[] delims)
        {
            if (string.IsNullOrEmpty(s))
            {
                return new List<string>();
            }
            return s.Split(delims).Where(o => !string.IsNullOrEmpty(o)).Select(o => o.Trim()).ToList();
        }

        protected void btnSyncImagePaths_Click(object sender, EventArgs e)
        {
            try
            {
                string lawIniFile = txtLawIniFile.Text;
                int lawDocID = Convert.ToInt32(txtLawDocID.Text);
                List<string> imagePaths = SplitStr(txtImagePaths.Text, "\r\n".ToCharArray());
                IEVLawAdapter adapter = EVLawAdapterFactory.NewEVLawAdapter(lawIniFile, "law_user", "law32sql");
                var lawDoc = new LawDocumentBEO()
                {
                    LawDocId = lawDocID,
                    ImagePaths = imagePaths
                };
                adapter.UpdateLawImagePaths(lawDoc);
                ltlResult.Text = "Update image paths complete";
               
            }
            catch (Exception ex)
            {
                ltlResult.Text = string.Format("Error Code: {0}\r\nUser Message: {1}\r\n{2}", ex.GetErrorCode(),
                    ex.ToUserString(), ex.ToString());

            }
        }

        protected void btnGetLawFields_Click(object sender, EventArgs e)
        {
            try
            {
                string lawIniFile = txtLawIniFile.Text;
                int lawDocID = Convert.ToInt32(txtLawDocID.Text);
                List<string> imagePaths = SplitStr(txtImagePaths.Text, "\r\n".ToCharArray());
                IEVLawAdapter adapter = EVLawAdapterFactory.NewEVLawAdapter(lawIniFile, "law_user", "law32sql");
                IEnumerable<LawFieldBEO> lawFields = adapter.GetFields();
                JavaScriptSerializer js = new JavaScriptSerializer();
                ltlResult.Text = js.Serialize(lawFields);

            }
            catch (Exception ex)
            {
                ltlResult.Text = string.Format("Error Code: {0}\r\nUser Message: {1}\r\n{2}", ex.GetErrorCode(),
                    ex.ToUserString(), ex.ToString());

            }
        }
    }
}