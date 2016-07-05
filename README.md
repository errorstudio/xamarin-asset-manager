# Howto
* Copy the code from the PCL, and the platform specific projects. 
* Change the default namespace if required (from AssetManager to MyGreatApp),
* In getFilePath(), Change the temp file store path from "local-content" to something more specific to your client domain.
* In isUpdateRequired and fetchAssets, change "client.tld/ping-url" and "client.tld/download-url" strings to your app specific url's
