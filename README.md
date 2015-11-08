# TsWspCompilation

Visual Studio 2015 Extension that adds TypeScript compilation support to website projects.

This extension will compile a Javascript file you are editing in a Visual Studio Website project, when you save it.

![TsWsp Options](https://knarfalingus.github.io/Content/TsWspCompilation/TsWspOptions.png)

If you are using a source control provider that sets files read only when not checked in, you can set the "aggressive checkout" feature which will cause the extension to issue the specified checkout command to try and check out files prior to modification. The command is provider specific and can be found in the Tools, Options window, Environment, Keyboard section of Visual Studio, below is the command that would be used for SourceGear Vault

![Studio Options](https://knarfalingus.github.io/Content/TsWspCompilation/VsOptions.PNG)
