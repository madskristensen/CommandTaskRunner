## Command Task Runner extension

Adds support for command line scripts in Visual Studio 2015's
Task Runner Explorer.

[![Build status](https://ci.appveyor.com/api/projects/status/grreswaawyla0j6c?svg=true)](https://ci.appveyor.com/project/madskristensen/commandtaskrunner)

Download the extension at the
[VS Gallery](https://visualstudiogallery.msdn.microsoft.com/9397a2da-c93a-419c-8408-4e9af30d4e36)
or get the
[nightly build](http://vsixgallery.com/extension/fc1aafb2-321e-41bd-ac37-03b09ea8ef31/)

### DNX scripts

Inside project.json it is possible to add custom scripts inside
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

### Execute scripts

When scripts are specified, the Task Runner Explorer
will show those scripts.

![Task list](art/task-list.png)

Each script can be executed by double-clicking the task.

### Bindings

Script bindings make it possible to associate individual scripts
with Visual Studio events such as "After build" etc.

![Visual Studio bindings](art/bindings.png)
