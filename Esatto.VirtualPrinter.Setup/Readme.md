# Installer

## Releasing a signed installer

```bat
signtool sign /sha1 49B27C9F8CA411C5EEB8C834A36D5A7DED9F9B02 /t http://timestamp.digicert.com /fd SHA256 /v Esatto.VirtualPrinter.Setup.msi
```