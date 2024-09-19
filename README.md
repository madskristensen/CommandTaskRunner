# Command Task Runner extension

Adds support for command line batch files in Visual Studio 2022's 
Task Runner Explorer. Supports `.exe`, `.cmd`, `.bat`, `.ps1` and `.psm1` files.

[![Build status](https://ci.appveyor.com/api/projects/status/grreswaawyla0j6c?svg=true)](https://ci.appveyor.com/project/madskristensen/commandtaskrunner)

Download the extension at the
[VS Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.CommandTaskRunner64)
or get the
[nightly build](http://vsixgallery.com/extension/fc1aafb2-321e-41bd-ac37-03b09ea8ef32/)

## Add commands

The easiest way to add a batch file to Task Runner Explorer
is to right-click it in Solution Explorer and select
**Add to Task Runner**

![Context menu](art/context-menu.png)

You can right-click supported batch files in either solution
folders or from within a any project.

Doing so will create a `commands.json` file. If you right-clicked
a batch file in a solution folder, then the `commands.json`
file will be placed in the solution folder. If the batch file
is in a project you will be prompted to select to either
put it in the project or solution folder.

If a `commands.json` file already exist, the new batch
file will be added.

## Execute scripts

When scripts are specified, the Task Runner Explorer
will show those scripts.

![Task list](art/task-list.png)

Each script can be executed by double-clicking the task.

## commands.json locations

The Task Runner Explorer supports multiple task runners in the
same solution. For instance, you can have commands specified
for the solution and additional ones for each project in that
solution.

Task Runner Explorer will try to find a `commands.json` file
in any parent folder to either the individual projects or
the solution until it hits the root of the drive.

You can also add a "commands.user.json" file if you need local, user specific commands. This file follows the same schema as the "commands.json" file. Any commands added to this file will appear under a "User Commands" group.


## Commands

Inside commands.json it is possible to add custom scripts inside
the "scripts" element.

```js
{
	"commands": {
		"Build": {
			"FileName": "cmd.exe",
			"WorkingDirectory": ".",
			"Arguments": "/c build\\build.cmd"
		}
	}
}
```

## Commands with no batch file

You can execute any command inside `commands.json` by manually
editing it. A batch file is not needed if you just need to
execute simple commands.

## Drag and drop

You can drag any supported batch file onto `commands.json`
to add it. Just keep in mind that Visual Studio doesn't support
drag and drop from solution folders.

## Bindings

Script bindings make it possible to associate individual scripts
with Visual Studio events such as "After build" etc.

![Visual Studio bindings](art/bindings.png)

## Limitations

Some projects (vcxproj (C++ and C++/cli projects), website folder projects, TwinCAT projects, etc.) do not propagate some properties regarding solution and project configuration that are needed by this extension. If there are no .NET project in the solution, this extension may not work at it's full capacity. At the moment, this extension works best with .NET projects.

## Open Command Line

For the optimal experience with batch file and Visual Studio, try
the free
[Open Command Line](https://visualstudiogallery.msdn.microsoft.com/4e84e2cf-2d6b-472a-b1e2-b84932511379)
extension for even more features.

## License
[Apache 2.0](LICENSE)
