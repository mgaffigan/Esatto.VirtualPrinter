<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"  xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl wix" xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*" />
    </xsl:copy>
  </xsl:template>
  
  <xsl:template match="wix:Class">
    <!-- ForeignServer is specified to avoid a WiX bug.  It does not show up in the generated MSI -->
    <Class Id="{@Id}" Description="{@Description}" ForeignServer="null">
      <xsl:apply-templates select="node()" />
    </Class>
  </xsl:template>

  <!--Set the LocalService entry for esVirtualPrinterRedirector -->
  <xsl:template match="wix:Component">
    <Component Id="{@Id}" Directory="{@Directory}" Guid="{@Guid}">
      <File Id="{wix:File/@Id}" KeyPath="yes" Source="{wix:File/@Source}" />
      <AppId Id="{wix:AppId/@Id}" LocalService="esVirtualPrinterRedirector">
        <xsl:apply-templates select="wix:AppId/node()" />
      </AppId>
      <ServiceInstall Id="Esatto.VirtualPrinter.Redirector_svc" Name="esVirtualPrinterRedirector" DisplayName="Esatto Virtual Printer Document Redirector" Description="Receives print documents from the print spooler and forwards them to a dispatcher running in the originating user's session.  This service is managed by COM and will start and stop as needed." Type="ownProcess" Start="demand" ErrorControl="normal">
        <ServiceDependency Id="RPCSS" />
      </ServiceInstall>
      <ServiceControl Id="Esatto.VirtualPrinter.Redirector_svc" Name="esVirtualPrinterRedirector" Remove="uninstall" Stop="uninstall" Wait="yes" />
      <xsl:copy-of select="wix:RegistryValue[not(@Name = 'LocalService')]" />
    </Component>
  </xsl:template>
</xsl:stylesheet>
