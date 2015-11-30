<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LawAdapter.aspx.cs" Inherits="LexisNexis.Evolution.Services.Test.LawAdapter" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        
        LAW Case ini file:
        <asp:TextBox runat="server" ID="txtLawIniFile" Width="600" Text="\\138.12.17.87\ImportSources\LAW\Mail Vs Thread\project.ini"></asp:TextBox><br />
        <asp:Button runat="server" ID="btnGetLawFields" Text="Get LAW Fields" OnClick="btnGetLawFields_Click"/><br/>
        Law fields names to syncs:
        <asp:TextBox runat="server" ID="txtFieldNames" Width="600" Text="EMail_Subject, _DocCat, DateSent, NingjunStr01, NingjunInt01, NingjunDate01"></asp:TextBox><br />
        Law fields types:
        <asp:TextBox runat="server" ID="txtFieldTypes" Text="string, int, date, string, int, date" Width="400"></asp:TextBox><br />

        Law fields values to syncs:
        <asp:TextBox runat="server" ID="txtFieldValues" Width="600" Text="Ningjun subject 001, 11, 2012-10-30 00:00:00, Value 01, 111, 2014-10-30 00:00:00"></asp:TextBox><br />

        Law tag names to syncs:
        <asp:TextBox runat="server" ID="txtTagNames" Text="EVTag01, EVTag02"></asp:TextBox><br />
        Law tag values to syncs:
        <asp:TextBox runat="server" ID="txtTagValues" Text="true, true"></asp:TextBox><br />

        Law Docs ID range:
        <asp:TextBox runat="server" ID="txtLawDocIDRange" Text="1 - 10"></asp:TextBox><br />

        <asp:Button runat="server" ID="btnSyncMetaData" Text="Sync Metadata" OnClick="btnSyncMetadata_Click" />
        <br />
        <h2>Update Image Path</h2>
        Law Doc ID:
        <asp:TextBox runat="server" ID="txtLawDocID" Text="2" Width="200" />
        <br />
        Image Paths:
        <asp:TextBox runat="server" ID="txtImagePaths" TextMode="MultiLine" Rows="10" Columns="50" 
            Text="EV_IMAGES\Job1001\0001\0001.tif
EV_IMAGES\Job1001\0001\0002.tif
EV_IMAGES\Job1001\0001\0003.tif" />            
        <br />
        <asp:Button runat="server" ID="btnSyncImagePaths" Text="Sync Image Paths" OnClick="btnSyncImagePaths_Click" />
        <pre>
            <asp:Literal runat="server" ID="ltlResult"></asp:Literal>
        </pre>
    </form>
</body>
</html>
