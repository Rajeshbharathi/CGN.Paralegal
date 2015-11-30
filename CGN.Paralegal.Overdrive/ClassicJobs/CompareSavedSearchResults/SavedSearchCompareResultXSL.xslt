<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" /><xsl:template match="/"><xsl:apply-templates/></xsl:template>
<xsl:template match="compare-report-details">Date comparison was run (current date),<xsl:value-of select="substring-before(created-date,'T')"/>
User that run the comparison,<xsl:value-of select="created-by"/>
Search syntax,<xsl:value-of select="search-query-term"/>
<xsl:apply-templates select="old-search-result-details"/>
<xsl:apply-templates select="new-search-result-details"/>
<xsl:apply-templates select="unique-old-documents"/>
<xsl:apply-templates select="unique-new-documents"/>
<xsl:apply-templates select="common-documents"/>
</xsl:template>
<xsl:template match="old-search-result-details">
Information about the saved result list
Date search was run ,<xsl:value-of select="substring-before(@createddate,'T')"/>
User who ran the query,<xsl:value-of select="@createdby"/>
Total No. of document, <xsl:value-of select="total-number-of-documents"/>
</xsl:template>
<xsl:template match="new-search-result-details">
Information about the current query
Date search was run ,<xsl:value-of select="substring-before(@createddate,'T')"/>
User who ran the query,<xsl:value-of select="@createdby"/>
Total No. of document, <xsl:value-of select="total-number-of-documents"/>
</xsl:template>
<xsl:template match="unique-old-documents">
Documents only in saved results list
Document Control Number,File name of each document
<xsl:apply-templates select="document"/>
</xsl:template>
<xsl:template match="unique-new-documents">
Documents only in new query
Document Control Number,File name of each document
<xsl:apply-templates select="document"/>
</xsl:template>
<xsl:template match="common-documents">
Documents in both queries
Document Control Number,File name of each document
<xsl:apply-templates select="document"/>
</xsl:template>
<xsl:template match="document">
<xsl:for-each select=".">
<xsl:value-of select="@document-control-number"/>,<xsl:value-of select="@document-nativepath"/><xsl:text>&#13;&#10;</xsl:text>
</xsl:for-each>
</xsl:template>
</xsl:stylesheet>