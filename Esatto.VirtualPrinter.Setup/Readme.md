# Installer

## Releasing a signed installer

The signing key is in Google Cloud KMS. Sign with jsign, not signtool.

One-time setup: install the Google Cloud CLI and jsign, then

```bat
gcloud auth activate-service-account --key-file "ITT Code Signing.json"
```

Then, per session (the token lasts about an hour):

```bat
gcloud auth print-access-token > token.txt

java -jar jsign.jar ^
    --storetype GOOGLECLOUD ^
    --keystore projects/itt-misc/locations/us/keyRings/itt ^
    --alias itt-2026-ev/cryptoKeyVersions/1 ^
    --storepass <access token> ^
    --certfile "ITT Code Signing.crt" ^
    --tsaurl http://timestamp.digicert.com ^
    --tsmode RFC3161 ^
    --replace ^
    <file>
```

Sign esattovp3.cat as well as the MSI. The timestamp flags are
required: without them the signature stops validating the day the
certificate expires.
