# Mobile Config Provisioner

This app parses Apple configuration profile (.mobileconfig_) files and applies them to the device it runs on.
It is meant as a simpler alternative to enrolling devices in MDM and pushing settins that way.

## Supported functionality

Currently only provisioning of Wi-Fi networks is supported, with the following limitations

- WPA2/3 Enterprise configuration *requires* that the profile includes trusted root CA certificates. Using the system's CA store - like is possible when configuring via Android's UI - is not supported by the Android API this app relies on.