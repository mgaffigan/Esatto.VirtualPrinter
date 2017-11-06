<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"  xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl wix" xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <!-- heat generates invalid code (https://github.com/wixtoolset/issues/issues/3412/) -->
  <xsl:template match="wix:Component">
    <Component Id="{@Id}" Directory="{@Directory}" Guid="{@Guid}">
      <File Id="{wix:File/@Id}" KeyPath="yes" Source="{wix:File/@Source}">
        <!-- 1. Missing language attribute -->
        <!-- 2. Typelib is supposed to be under file, but is generated under Component -->
        <TypeLib Id="{wix:TypeLib/@Id}" Description="{wix:TypeLib/@Description}" Language="1033" MajorVersion="{wix:TypeLib/@MajorVersion}" MinorVersion="{wix:TypeLib/@MinorVersion}">
          <xsl:apply-templates select="wix:TypeLib/node()" />
        </TypeLib>
      </File>
      <xsl:copy-of select="wix:RegistryValue[not(@Name = 'LocalService')]" />
    </Component>
  </xsl:template>

</xsl:stylesheet>
