# Servant

Servant is a piece of software that transforms your regular Internet Information Services (IIS) Manager to a beautiful, fast and web-based management tool.

It's designed to fit your daily routines, optimized to track down your worst problems and created to stop you from wasting time on administration.

## Two Versions
Servant is available in two versions. The regular version with a simple self-hosted interface or a thin client communicating directly with Servant.io using a secured WebSocket connection. Both versions are open source, however, the thin client is only to use with Servant.io.

### Requirements
There are a few minimum requirements for Servant:

* ASP.NET 4.0
* Works only with IIS7 and up

### Servant.Server
You simply download Servant and extract it to a folder of your choice. This version is also possible to use with Servant.io, but also provides a hosted interface, but not the same features that Servant.io offers.
As of now, this version is *not automatically updated*.

#### Installation
1. Download a zip (the green button) of the latest stable release from [the releases page](https://github.com/jhovgaard/servant/releases).
2. Extract files to desired folder.
3. Double-click the file "Install Servant Service.bat"
4. Follow instructions on screen.

### Servant.Client

This is a lightweight Servant edition only to be used with Servant.io. When using the Servant.io platform, more features are supported.
The client is automatically updated and requires no user interaction.

#### Installation

1. Download and run the MSI installer from [the releases page](https://github.com/jhovgaard/servant/releases).
2. Key in your Servant.io key found in your dashboard.
3. Complete the installation. The service is automatically started and the server should be visible within seconds.


License
--------------
Servant is licensed under MIT. Refer to license.txt for more information.
