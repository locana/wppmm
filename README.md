WPPMM
=====
- Remote shooting application for Windows Phone 8 powered by [Sony Camera Remote API beta](http://developer.sony.com/develop/cameras/).
- Released in Windows Phone Apps Store as [Scrap](http://www.windowsphone.com/en-us/store/app/scrap/896b0e1b-2c1a-40e4-9c55-09050e3860dc). Play with your Sony camera devices and give us feedbacks.

This software is published under the [MIT License](http://opensource.org/licenses/mit-license.php).

##Build
1. Clone repositories.
 ``` bash
 git clone git@github.com:kazyx/WPPMM.git
 cd WPMMM
 git submodule update --init
 ```

2. Open /Project/PhoneApp.sln by Visual Studio 2012 for WP.

3. Add reference of [Json.NET](https://github.com/JamesNK/Newtonsoft.Json) to KzRemoteApi project.

4. Add reference of [The Windows Phone Toolkit](http://phone.codeplex.com/) to PhoneApp project.
