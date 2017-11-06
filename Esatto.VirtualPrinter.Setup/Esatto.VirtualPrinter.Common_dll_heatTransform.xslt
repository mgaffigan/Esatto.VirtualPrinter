<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"  xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl wix" xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <!-- Search for elements that have a file source, and add an attribute that will let wix GAC them -->
  <xsl:template match="wix:File[contains(@Source, 'dll')]">
    <File Id="{@Id}" KeyPath="yes" Source="{@Source}" Assembly=".net" />
  </xsl:template>
  
  <!-- Prevent codebase from being emitted -->
  <xsl:template match="wix:RegistryValue[@Name = 'CodeBase']" />

</xsl:stylesheet>
