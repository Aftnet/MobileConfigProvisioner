# Mobile Config Provisioner

This app parses Apple configuration profile (.mobileconfig) files and applies them to the device it runs on.
It is meant as a simpler alternative to enrolling devices in MDM and pushing settings that way.

## Supported functionality

Currently only provisioning of Wi-Fi networks is supported, with the following limitations

- WPA2/3 Enterprise configuration *requires* that the profile includes trusted root CA certificates. Using the system's CA store - like is possible when configuring via Android's UI - is not supported by the Android API this app relies on.

## How to use

1. Install APK from this repo's releases to device
2. Copy a .mobileconfig file to the device
3. Open app, select profile file
4. Accept configuration of new Wi-Fi networks