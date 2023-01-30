# InjectionTimeGetterApp

This console line application is used to get MS1 and MS2 injection times from Thermo .raw files. It uses mzLib to load and get injection times. The source code for mzLib can be found at https://github.com/smith-chem-wisc/mzLib. Other file formats can be supported in the future, if there is large enough interest. 

## Requisites for running
1) .NET 6.0 (https://dotnet.microsoft.com/en-us/download)
2) Visual Studio (https://visualstudio.microsoft.com/#vs-section)
3) At least 16 GB of RAM. 
4) Windows 11. Issues causes by running on Linux-based machines will not be supported.  

## Building InjectionTimeGetterApp
1) Clone this repository to your local machine. 
2) Navigate to the directory on your local machine and open the InjectionTimeGetterApp.sln in Visual Studio. 
3) Build the application either for debug or release mode (Ctrl + Shift + B, or the first option in the Build dropdown menu).  
4) Make note of the final location of the .exe file output by release, which will be found in InjectionTimeGetterApp\bin\Debug\net6.0\. 

## Running InjectionTimeGetterApp
1) open command prompt. 
2) type the following command 
> start /b "" "{path to InjectionTimeGetterApp.exe}" "0" "{MS LEVEL}" "{PATH TO FOLDER OF DATA}"
