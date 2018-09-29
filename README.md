# ![Open TortoiseSVN logo](https://raw.githubusercontent.com/masamitsu-murase/open_tortoise_svn_for_microsoft_edge/master/extension/icons/icon48.png) Open TortoiseSVN for Microsoft Edge

## Overview

This extension enables you to open a file directly in [TortoiseSVN](http://tortoisesvn.tigris.org/) instead of in the browser.  
When you click on a link to one of the registered URLs, TortoiseSVN Repository Browser will open.

This extension also displays some contextual menus to open TortoiseSVN Repository Browser, Log Viewer and Blame Viewer when right clicking on a URL.

This extension is a Microsoft Edge version of [Open TortoiseSVN for Firefox](https://addons.mozilla.org/en/firefox/addon/open-tortoisesvn/).

## How to install.

1. Confirm that your Edge is "Microsoft EdgeHTML 15" or later.
2. Download [appx file](https://github.com/masamitsu-murase/open_tortoise_svn_for_microsoft_edge/blob/master/package/OpenTortoiseSVN.appx?raw=true) and [certificate file](https://github.com/masamitsu-murase/open_tortoise_svn_for_microsoft_edge/blob/master/package/public.cer?raw=true).
3. Add the certificate to your certificate store with the following command.  
   ```
   > certutil -addstore TrustedPeople public.cer
   ```
   Run this command as an administrator.
4. Install the appx with the following command in **PowerShell**.
   ```
   > Add-AppxPackage OpenTortoiseSVN.appx
   ```
   Run this command as an administrator.  
   If `Add-AppxPackage` causes any error, you may have to enable *Developer Mode*. Please refer to [Microsoft page](https://docs.microsoft.com/en-US/windows/uwp/get-started/enable-your-device-for-development).
5. You can find this extension in Edge.

## For Developers

This extension uses [Native Messaging](https://docs.microsoft.com/en-us/microsoft-edge/extensions/guides/native-messaging).

Native Messaging is a feature supported by Microsoft Edge.  
It communicates with the UWP App, which is included in this extension.

## License

You may use this software under the terms of the MIT License.

Copyright (c) 2017 Masamitsu MURASE

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

